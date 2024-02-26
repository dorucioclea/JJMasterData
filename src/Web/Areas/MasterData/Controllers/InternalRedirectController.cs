﻿using System.Web;
using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Commons.Exceptions;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Security.Cryptography.Abstractions;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataManager.Expressions;
using JJMasterData.Core.DataManager.Models;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.UI.Components;
using JJMasterData.Web.Areas.MasterData.Models;
using JJMasterData.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Web.Areas.MasterData.Controllers;

public class InternalRedirectController(
    ExpressionsService expressionsService,
    IComponentFactory componentFactory, 
    IStringLocalizer<MasterDataResources> localizer,
    IEncryptionService encryptionService) : MasterDataController
{
    private string? _elementName;
    private RelationshipViewType _relationshipType;
    private Dictionary<string, object> RelationValues { get; } = new();

    public async Task<IActionResult> Index(string parameters)
    {
        LoadParameters(parameters);
        var userId = HttpContext.GetUserId();

        InternalRedirectViewModel model;

        switch (_relationshipType)
        {
            case RelationshipViewType.List:
            {
                var formView = await componentFactory.FormView.CreateAsync(_elementName);

                formView.RelationValues = RelationValues;

                if (userId != null)
                {
                    formView.SetUserValues("USERID", userId);
                    formView.GridView.SetCurrentFilter("USERID", userId);
                }
                
                var result = await formView.GetResultAsync();

                if (result is IActionResult actionResult)
                    return actionResult;

                var title = expressionsService.GetExpressionValue(formView.FormElement.Title, new FormStateData(RelationValues!, PageState.List))?.ToString();
                model = new(title ?? formView.Name,result.Content!, false);
                break;
            }
            case RelationshipViewType.View:
            {
                var panel = await componentFactory.DataPanel.CreateAsync(_elementName);
                panel.PageState = PageState.View;
                if (userId != null)
                    panel.SetUserValues("USERID", userId);

                await panel.LoadValuesFromPkAsync(RelationValues);
                
                var result = await panel.GetResultAsync();

                if (result is IActionResult actionResult)
                    return actionResult;
                
                var title = expressionsService.GetExpressionValue(panel.FormElement.Title, new FormStateData(RelationValues!, PageState.View))?.ToString();

                model = new(title ?? panel.Name,result.Content!, false);
                
                break;
            }
            case RelationshipViewType.Update:
            {
                var panel = await componentFactory.DataPanel.CreateAsync(_elementName);
                panel.PageState = PageState.Update;

                await panel.LoadValuesFromPkAsync(RelationValues);
                
                var result = await panel.GetResultAsync();

                if (userId != null)
                    panel.SetUserValues("USERID", userId);
                
                if (result is IActionResult actionResult)
                    return actionResult;
                
                var title = expressionsService.GetExpressionValue(panel.FormElement.Title, new FormStateData(RelationValues!, PageState.Update))?.ToString();

                model = new(title ?? panel.Name,result.Content, true);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Save(string parameters)
    {
        LoadParameters(parameters);

        var userId = HttpContext.GetUserId();

        var panel = await componentFactory.DataPanel.CreateAsync(_elementName);
        panel.PageState = PageState.Update;

        await panel.LoadValuesFromPkAsync(RelationValues);
        if (userId != null)
            panel.SetUserValues("USERID", userId);

        var values = await panel.GetFormValuesAsync();
        var errors =  panel.ValidateFields(values, PageState.Update);
        var formElement = panel.FormElement;
        try
        {
            if (errors.Count == 0)
                await panel.EntityRepository.SetValuesAsync(formElement, values);
        }
        catch (SqlException ex)
        {
            errors.Add("DB", localizer[ExceptionManager.GetMessage(ex)]);
        }

        if (errors.Count > 0)
        {
            ViewBag.Error = componentFactory.Html.ValidationSummary.Create(errors).GetHtml();
            ViewBag.Success = false;
        }
        else
        {
            ViewBag.Success = true;
        }

        var result = await panel.GetResultAsync();
                        
        if (result is IActionResult actionResult)
            return actionResult;
                
        var title = expressionsService.GetExpressionValue(panel.FormElement.Title, new FormStateData(RelationValues!, PageState.Update))?.ToString();

        var model = new InternalRedirectViewModel(title ?? panel.Name,result.Content, true);

        return View("Index", model);
    }

    private void LoadParameters(string parameters)
    {
        if (string.IsNullOrEmpty(parameters))
            throw new ArgumentNullException();

        _elementName = null;
        _relationshipType = RelationshipViewType.List;
        var @params = HttpUtility.ParseQueryString(encryptionService.DecryptStringWithUrlUnescape(parameters));
        _elementName = @params.Get("formname");
        foreach (string key in @params)
        {
            switch (key.ToLower())
            {
                case "formname":
                    _elementName = @params.Get(key);
                    break;
                case "viewtype":
                    _relationshipType = (RelationshipViewType)int.Parse(@params.Get(key) ?? string.Empty);
                    break;
                default:
                    RelationValues.Add(key, @params.Get(key)!);
                    break;
            }
        }
    }
}
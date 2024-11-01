﻿using System.Web;
using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Commons.Security.Cryptography.Abstractions;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Expressions;
using JJMasterData.Core.DataManager.Models;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.Http.Abstractions;
using JJMasterData.Core.UI.Components;
using JJMasterData.Web.Areas.MasterData.Models;
using Microsoft.AspNetCore.Mvc;

namespace JJMasterData.Web.Areas.MasterData.Controllers;

public class InternalRedirectController(
    ExpressionsService expressionsService,
    IComponentFactory componentFactory, 
    FormService formService,
    IHttpRequest request,
    IEncryptionService encryptionService) : MasterDataController
{
    public async Task<IActionResult> Index(string parameters)
    {
        var state = GetInternalRedirectState(parameters);
        var userId = HttpContext.User.GetUserId();
        InternalRedirectViewModel model;

        switch (state.RelationshipType)
        {
            case RelationshipViewType.List:
            {
                var formView = await componentFactory.FormView.CreateAsync(state.ElementName);
                formView.RelationValues = state.RelationValues;

                if (userId != null)
                {
                    formView.SetUserValues("USERID", userId);
                    formView.GridView.SetCurrentFilter("USERID", userId);
                }
                
                var result = await formView.GetResultAsync();
                if (result is IActionResult actionResult)
                    return actionResult;

                var title = expressionsService.GetExpressionValue(formView.FormElement.Title, new FormStateData(state.RelationValues!, PageState.List))?.ToString();
                model = new(title ?? formView.Name, result.Content!, false);
                break;
            }
            case RelationshipViewType.View:
            {
                var panel = await componentFactory.DataPanel.CreateAsync(state.ElementName);
                panel.PageState = PageState.View;
                
                if (userId != null)
                    panel.SetUserValues("USERID", userId);

                await panel.LoadValuesFromPkAsync(state.RelationValues);
                
                DataHelper.CopyIntoDictionary(panel.Values, state.RelationValues!);
                
                var result = await panel.GetResultAsync();
                if (result is IActionResult actionResult)
                    return actionResult;
                
                var title = expressionsService.GetExpressionValue(panel.FormElement.Title, new FormStateData(state.RelationValues!, PageState.View))?.ToString();
                model = new(title ?? panel.Name, result.Content!, false);
                break;
            }
            case RelationshipViewType.Update:
            {
                var panel = await componentFactory.DataPanel.CreateAsync(state.ElementName);
                panel.PageState = PageState.Update;
            
                await panel.LoadValuesFromPkAsync(state.RelationValues);
                
                DataHelper.CopyIntoDictionary(panel.Values, state.RelationValues!);
                
                if (userId != null)
                    panel.SetUserValues("USERID", userId);
                
                var result = await panel.GetResultAsync();
                if (result is IActionResult actionResult)
                    return actionResult;
                
                var title = expressionsService.GetExpressionValue(panel.FormElement.Title, new FormStateData(state.RelationValues!, PageState.Update))?.ToString();
                model = new(title ?? panel.Name, result.Content, true);
                break;
            }
            default:
                throw new InvalidOperationException();
        }

        return View(model);
    }
    
    [HttpPost]
    public async Task<IActionResult> Save(string parameters)
    {
        var state = GetInternalRedirectState(parameters);
        var userId = HttpContext.User.GetUserId();
        var panel = await componentFactory.DataPanel.CreateAsync(state.ElementName);
        panel.PageState = PageState.Update;

        await panel.LoadValuesFromPkAsync(state.RelationValues);
        if (userId != null)
            panel.SetUserValues("USERID", userId);

        var values = await panel.GetFormValuesAsync();
        var letter = await formService.InsertOrReplaceAsync(panel.FormElement, values, new DataContext(request, DataContextSource.Form, userId));

        ViewBag.Success = letter.Errors.Count == 0;
        if (letter.Errors.Count > 0)
            ViewBag.Error = componentFactory.Html.ValidationSummary.Create(letter.Errors).GetHtml();

        var result = await panel.GetResultAsync();
        if (result is IActionResult actionResult)
            return actionResult;

        var title = expressionsService.GetExpressionValue(panel.FormElement.Title, new FormStateData(state.RelationValues!, PageState.Update))?.ToString();
        var model = new InternalRedirectViewModel(title ?? panel.Name, result.Content, true);

        return View("Index", model);
    }
    
    private InternalRedirectState GetInternalRedirectState(string parameters)
    {
        if (string.IsNullOrEmpty(parameters))
            throw new ArgumentNullException(nameof(parameters));

        var state = new InternalRedirectState
        {
            RelationshipType = RelationshipViewType.List
        };

        var @params = HttpUtility.ParseQueryString(encryptionService.DecryptStringWithUrlUnescape(parameters));
        state.ElementName = @params.Get("formname");

        foreach (string key in @params)
        {
            switch (key.ToLower())
            {
                case "formname":
                    state.ElementName = @params.Get(key);
                    break;
                case "viewtype":
                    state.RelationshipType = (RelationshipViewType)int.Parse(@params.Get(key) ?? string.Empty);
                    break;
                default:
                    state.RelationValues.Add(key, @params.Get(key)!);
                    break;
            }
        }

        return state;
    }
}
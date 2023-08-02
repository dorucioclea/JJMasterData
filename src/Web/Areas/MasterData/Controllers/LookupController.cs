﻿using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Actions.Abstractions;
using JJMasterData.Core.DataDictionary.Actions.GridToolbar;
using JJMasterData.Core.DataDictionary.Actions.UserCreated;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Factories;
using JJMasterData.Web.Areas.MasterData.Models;
using JJMasterData.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Web.Areas.MasterData.Controllers;

public class LookupController : MasterDataController
{
    private  IFormElementComponentFactory<JJFormView> FormViewFactory { get; }
    private IDataDictionaryRepository DataDictionaryRepository { get; }
    private ILookupService LookupService { get; }
    private IFormValuesService FormValuesService { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }

    public LookupController(
        IFormElementComponentFactory<JJFormView> formViewFactory,
        IDataDictionaryRepository dataDictionaryRepository,
        ILookupService lookupService,
        IFormValuesService formValuesService,
        IStringLocalizer<JJMasterDataResources> stringLocalizer
        )
    {
        FormViewFactory = formViewFactory;
        DataDictionaryRepository = dataDictionaryRepository;
        LookupService = lookupService;
        FormValuesService = formValuesService;
        StringLocalizer = stringLocalizer;
    }
    
    [ServiceFilter<LookupParametersDecryptionFilter>]
    public IActionResult Index(LookupParameters lookupParameters)
    {
        return View(new LookupViewModel
        {
            LookupParameters = lookupParameters,
            LookupFormConfiguration = ConfigureLookupForm
        }); 
    }

    [ServiceFilter<FormElementDecryptionFilter>]
    public async Task<IActionResult> GetResult(
        FormElement formElement, 
        string componentName,
        PageState pageState,
        string fieldName,
        string searchId)
    {
        var dataItem = formElement.Fields[fieldName].DataItem;

        var formValues = await FormValuesService.GetFormValuesWithMergedValuesAsync(formElement,pageState,true);

        var selectedValue = LookupService.GetSelectedValue(componentName).ToString();
        
        var description = await LookupService.GetDescriptionAsync(dataItem,selectedValue,pageState,formValues,false);
        return Json(new LookupResultDto(searchId,description));
    }

    private void ConfigureLookupForm(JJFormView form, LookupParameters lookupParameters)
    {
        form.ShowTitle = false;

        if (!lookupParameters.EnableElementActions)
        {
            foreach (var action in form.GridView.ToolBarActions.Where(IsLookupAction()))
            {
                action.SetVisible(false);
            }

            foreach (var action in form.GridView.GridActions)
            {
                action.SetVisible(false);
            }
        }

        foreach (var action in form.GridView.GridActions)
        {
            action.IsDefaultOption = false;
        }

        var script = $"FormView.setLookup('{lookupParameters.ComponentName}','{{{lookupParameters.FieldKey}}}');";
        var selAction = new ScriptAction
        {
            Name = "jjselLookup",
            Icon = IconType.ChevronRight,
            ToolTip = StringLocalizer["Select"],
            OnClientClick = script,
            IsDefaultOption = true,
            Order = 100
        };
        form.GridView.GridActions.Add(selAction);

        foreach (var filter in lookupParameters.Filters)
        {
            form.SetCurrentFilter(filter.Key, filter.Value?.ToString());
        }
    }

    private static Func<BasicAction, bool> IsLookupAction()
    {
        return action => action is not LegendAction 
                         && action is not RefreshAction 
                         && action is not FilterAction 
                         && action is not ConfigAction 
                         && action is not SortAction;
    }
}
﻿#nullable enable

using System.Text.Json;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataDictionary.Services;
using JJMasterData.Core.UI.Html;
using JJMasterData.Web.Extensions;
using JJMasterData.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;


namespace JJMasterData.Web.Areas.DataDictionary.Controllers;

public class FieldController(
    FieldService fieldService,
    IStringLocalizer<MasterDataResources> stringLocalizer
    )
    : DataDictionaryController
{
    [ImportModelState]
    public async Task<IActionResult> Index(string elementName, string? fieldName)
    {
        var formElement = await fieldService.GetFormElementAsync(elementName);
        FormElementField? field;

        if (string.IsNullOrEmpty(fieldName))
        {
            if (TempData.ContainsKey("field"))
                field = TempData.Get<FormElementField>("field");
            else if (formElement.Fields.Count > 0)
                field = formElement.Fields[0];
            else
                field = new FormElementField();
        }
        else
        {
            field = formElement.Fields[fieldName];
        }

        await PopulateViewData(formElement, field);
        return View(nameof(Index), field);
    }

    public async Task<IActionResult> Detail(string elementName, string fieldName)
    {
        var formElement = await fieldService.GetFormElementAsync(elementName);
        var field = formElement.Fields[fieldName];
        await PopulateViewData(formElement, field);
        return PartialView("_Detail", field);
    }

    public async Task<IActionResult> Add(string elementName)
    {
        var formElement = await fieldService.GetFormElementAsync(elementName);
        var field = new FormElementField();
        await PopulateViewData(formElement, field);
        return PartialView("_Detail", field);
    }

    public async Task<IActionResult> Delete(string elementName, string fieldName)
    {
        await fieldService.DeleteField(elementName, fieldName);
        var nextField = await fieldService.GetNextFieldNameAsync(elementName, fieldName);
        return RedirectToAction("Index", new { elementName, fieldName = nextField });
    }

    [HttpPost]
    public async Task<IActionResult> Index(string elementName, string? fieldName, FormElementField? field)
    {
        var formElement = await fieldService.GetFormElementAsync(elementName);
        await PopulateViewData(formElement, field);
        return View("Index", field);
    }

    [HttpPost]
    [ExportModelState]
    public async Task<IActionResult> Save(string elementName, FormElementField field, string? originalName)
    {
        RecoverCustomAttributes(ref field);

        await fieldService.SaveFieldAsync(elementName, field, originalName);
        if (ModelState.IsValid)
        {
            return RedirectToIndex(elementName, field);
        }
        
        return RedirectToIndex(elementName, field);
    }


    [HttpPost]
    public async Task<IActionResult> Sort(string elementName, string fieldsOrder)
    {
        await fieldService.SortFieldsAsync(elementName, fieldsOrder.Split(","));
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Copy(string elementName, FormElementField? field)
    {
        var dictionary = await fieldService.GetFormElementAsync(elementName);
        await fieldService.CopyFieldAsync(dictionary, field);
        if (!ModelState.IsValid)
            ViewBag.Error = fieldService.GetValidationSummary().GetHtml();

        await PopulateViewData(dictionary, field);
        return View("Index", field);
    }

    [HttpPost]
    public IActionResult AddDataItem(string elementName, FormElementField field, int qtdRowsToAdd)
    {
        field.DataItem ??= new FormElementDataItem();
        field.DataItem.Items ??= [];
        for (int i = 0; i < qtdRowsToAdd; i++)
        {
            var item = new DataItemValue
            {
                Id = field.DataItem.Items.Count.ToString(),
                Description = "",
                Icon = IconType.Star,
                IconColor = "#ffffff"
            };
            field.DataItem.Items.Add(item);
        }

        return RedirectToIndex(elementName, field);
    }

    [HttpPost]
    public IActionResult RemoveDataItem(string elementName, FormElementField field, int dataItemIndex)
    {
        field.DataItem?.Items?.RemoveAt(dataItemIndex);
        return RedirectToIndex(elementName, field);
    }

    [HttpPost]
    public IActionResult RemoveAllDataItem(string elementName, FormElementField field)
    {
        field.DataItem!.Items = new List<DataItemValue>();
        return RedirectToIndex(elementName, field);
    }

    [HttpPost]
    public async Task<IActionResult> AddElementMapFilter(string elementName, FormElementField field,
        DataElementMapFilter elementMapFilter)
    {
        bool isValid = await fieldService.AddElementMapFilterAsync(field, elementMapFilter);
        if (!isValid)
        {
            ViewBag.Error = fieldService.GetValidationSummary().GetHtml();
        }

        return RedirectToIndex(elementName, field);
    }

    [HttpPost]
    public IActionResult RemoveElementMapFilter(string elementName, FormElementField field, string elementMapFieldName)
    {
        field.DataItem!.ElementMap!.MapFilters.RemoveAll(x => x.FieldName.Equals(elementMapFieldName));
        return RedirectToIndex(elementName, field);
    }

    private RedirectToActionResult RedirectToIndex(string elementName, FormElementField field)
    {
        TempData.Put("field", field);
        TempData["Error"] = ViewData["Error"];
        TempData["selected-tab"] = Request.Form["selected-tab"].ToString();
        TempData["OriginalName"] = ModelState.IsValid ? field.Name : Request.Form["originalName"].ToString();

        return RedirectToAction("Index", new { elementName });
    }

    private async Task PopulateViewData(FormElement formElement, FormElementField? field)
    {
        if (formElement == null)
            throw new ArgumentNullException(nameof(formElement));
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        field.DataItem ??= new FormElementDataItem();
        field.DataFile ??= new FormElementDataFile
        {
            MaxFileSize = 2
        };

        //Refresh action
        if (formElement.Fields.Contains(field.Name))
            field.Actions = formElement.Fields[field.Name].Actions;

        if (Request.HasFormContentType && Request.Form.TryGetValue("selected-tab", out var selectedTab))
            ViewData["Tab"] = selectedTab;

        else if (TempData.TryGetValue("selected-tab", out var tempSelectedTab))
            ViewData["Tab"] = tempSelectedTab?.ToString()!;

        if (TempData.TryGetValue("Error", out var value))
            ViewData["Error"] = value!;

        if (Request.HasFormContentType && Request.Form.TryGetValue("originalName", out var originalName))
            ViewData["OriginalName"] = originalName;
        else if (TempData.TryGetValue("originalName", out var tempOriginalName))
            ViewData["OriginalName"] = tempOriginalName?.ToString()!;

        if (TempData["OriginalName"] != null)
            ViewData["OriginalName"] = TempData["originalName"]!;
        else
            ViewData["OriginalName"] = field.Name;

        ViewData["IsForm"] = formElement.TypeIdentifier == 'F';
        ViewData["MenuId"] = "Fields";
        ViewData["FormElement"] = formElement;
        ViewData["ElementName"] = formElement.Name;
        ViewData["CodeMirrorHintList"] = JsonSerializer.Serialize(DataDictionaryServiceBase.GetAutocompleteHints(formElement));

        // ASP.NET Core enforces 30MB (~28.6 MiB) max request body size limit, be it Kestrel and HttpSys.
        // Under normal circumstances, there is no need to increase the size of the HTTP request.
        ViewData["MaxRequestLength"] = 30720000;

        ViewData["FieldName"] = field.Name;
        ViewData["Fields"] = formElement.Fields;
        ViewData["ElementNameList"] = await fieldService.GetElementsDictionaryAsync();
        
        if (field.Component is not FormComponent.Lookup &&
            field.Component is not FormComponent.Search &&
            field.Component is not FormComponent.ComboBox &&
            field.Component is not FormComponent.RadioButtonGroup)
        {
            return;
        }

        field.DataItem.ElementMap ??= new DataElementMap();


        ViewData["ElementFieldList"] = await fieldService.GetElementFieldListAsync(field.DataItem.ElementMap.ElementName);
    }

    private void RecoverCustomAttributes(ref FormElementField field)
    {
        field.Attributes = new Dictionary<string, object?>();
        switch (field.Component)
        {
            case FormComponent.Text:
            case FormComponent.Number:
            case FormComponent.Password:
            case FormComponent.Email:
            case FormComponent.Cnpj:
            case FormComponent.Cpf:
            case FormComponent.Slider:
            case FormComponent.CnpjCpf:
            case FormComponent.Cep:
                field.SetAttr(FormElementField.PlaceholderAttribute, Request.Form["txtPlaceHolder"].ToString());

                if (field.Component is FormComponent.Number or FormComponent.Slider)
                {
                    if (double.TryParse(Request.Form["step"], out var step))
                    {
                        field.SetAttr(FormElementField.StepAttribute, step);
                    }

                    if (double.TryParse(Request.Form["minValue"], out var minValue))
                    {
                        field.SetAttr(FormElementField.MinValueAttribute, minValue);
                    }

                    if (double.TryParse(Request.Form["maxValue"], out var maxValue))
                    {
                        field.SetAttr(FormElementField.MaxValueAttribute, maxValue);
                    }
                }

                break;
            case FormComponent.TextArea:
                field.SetAttr(FormElementField.RowsAttribute, Request.Form["txtTextAreaRows"].ToString());
                break;
            case FormComponent.ComboBox:
            case FormComponent.Search:
            case FormComponent.Lookup:
                field.SetAttr(FormElementField.PopUpSizeAttribute, Request.Form["cboLkPopUpSize"].ToString());
                field.SetAttr(FormElementField.PopUpTitleAttribute, Request.Form["txtLkPopUpTitle"].ToString());
                break;
            case FormComponent.CheckBox:
                switch (Request.Form["checkbox-layout"])
                {
                    case "checkbox":
                        field.SetAttr(FormElementField.IsSwitchAttribute, false);
                        field.SetAttr(FormElementField.IsButtonAttribute, false);
                        break;
                    case "switch":
                        field.SetAttr(FormElementField.IsSwitchAttribute, true);
                        field.SetAttr(FormElementField.IsButtonAttribute, false);
                        break;
                    case "button":
                        field.SetAttr(FormElementField.IsSwitchAttribute, false);
                        field.SetAttr(FormElementField.IsButtonAttribute, true);
                        break;
                }
                break;
            case FormComponent.Date
                or FormComponent.DateTime
                or FormComponent.Hour:
                field.SetAttr(FormElementField.AutocompletePickerAttribute,
                    Request.Form[FormElementField.AutocompletePickerAttribute] == "true");
                break;
            case FormComponent.Currency:
                field.SetAttr(FormElementField.CultureInfoAttribute,
                    Request.Form[FormElementField.CultureInfoAttribute].ToString());
                break;
        }
    }

    public async Task<ContentResult> PopulateCopyFromFields(string elementName)
    {
        var options = new HtmlBuilder();

        var formElement = await fieldService.GetFormElementAsync(elementName);
        
        foreach (var field in formElement.Fields)
        {
            options.Append(HtmlTag.Option, option =>
            {
                option.WithValue(field.Name);
                option.AppendText(field.Name);
            });
        }

        return Content(options.ToString());
    }
    
    public async Task<RedirectToActionResult> CopyFrom(string elementName, string copyFromElementName, string copyFromFieldName)
    {
        var copyFromFormElement = await fieldService.GetFormElementAsync(copyFromElementName);
        var field = copyFromFormElement.Fields[copyFromFieldName];

        var formElement = await fieldService.GetFormElementAsync(elementName);

        if (formElement.Fields.Exists(field.Name))
        {
            ModelState.AddModelError(field.Name,stringLocalizer["Field {0} already exists.", field.Name]);
        }
        else
        {
            formElement.Fields.Add(field);
        }

        await fieldService.SetFormElementAsync(formElement);

        if (!ModelState.IsValid)
            ViewData["Error"] = fieldService.GetValidationSummary().GetHtml();
        
        return RedirectToIndex(elementName, field);
    }
}
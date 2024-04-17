using JJMasterData.Core.DataDictionary.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Commons.Data.Entity.Repository.Abstractions;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.UI.Events.Args;
using JJMasterData.Web.Areas.DataDictionary.Models;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace JJMasterData.Web.Areas.DataDictionary.Controllers;

public class ElementController(
    ElementService elementService,
    ClassGenerationService classGenerationService,
    ScriptsService scriptsService,
    IEntityRepository entityRepository,
    IComponentFactory<JJUploadArea> uploadAreaFactory,
    IOptionsSnapshot<MasterDataCommonsOptions> masterDataOptions,
    IStringLocalizer<MasterDataResources> stringLocalizer)
    : DataDictionaryController
{
    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(masterDataOptions.Value.ConnectionString))
            return RedirectToAction("Index", "Settings");
            
        await elementService.CreateStructureIfNotExistsAsync();
        
        var formView = elementService.GetFormView();
        var result = await formView.GetResultAsync();
        
        if (result is IActionResult actionResult)
            return actionResult;

        ViewBag.FormViewHtml = result.Content!;
        
        return View();
    }

    public IActionResult Add()
    {
        return View();
    }

    public async Task<IActionResult> Export()
    {
        var formView = elementService.GetFormView();
        var selectedRows = formView.GridView.GetSelectedGridValues();

        if (selectedRows.Count == 1)
        {
            var jsonBytes = await elementService.ExportSingleRowAsync(selectedRows[0]);
            var jsonFileName = $"{selectedRows[0]["name"]}.json";

            Response.Headers["Content-Disposition"] =  $"attachment; filename=\"{jsonFileName}\"";
            
            return File(jsonBytes, "application/octet-stream");
        }

        var zipBytes = await elementService.ExportMultipleRowsAsync(selectedRows);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var zipFileName = $"DataDictionaryExportation_{timestamp}.zip";

        Response.Headers["Content-Disposition"] = $"attachment; filename=\"{zipFileName}\"";
        
        return File(zipBytes, "application/octet-stream");
    }

    public async Task<IActionResult> Import()
    {
        var uploadArea = uploadAreaFactory.Create();
        
        ConfigureUploadArea(uploadArea);
        
        var result = await uploadArea.GetResultAsync();

        if (result is IActionResult actionResult)
            return actionResult;
        
        return PartialView(new ImportViewModel(result.Content));
    }

    private void ConfigureUploadArea(JJUploadArea upload)
    {
        upload.AllowedTypes = "json";
        upload.JsCallback = "importationCallback()";
        upload.Url = Url.Action("Import", "Element");
        upload.OnFileUploadedAsync += FileUploaded;
    }

    private async Task FileUploaded(object? sender, FormUploadFileEventArgs e)
    {
        await elementService.Import(new MemoryStream(e.File.Bytes));
        if (ModelState.IsValid)
        {
            e.SuccessMessage = stringLocalizer["Dictionary imported successfully!"];
        }
        else
        {
            var jjSummary = elementService.GetValidationSummary();
            foreach (var err in jjSummary.Errors)
                e.ErrorMessage += $"<br>{err}";
        }
    }

    public IActionResult Duplicate(string? elementName = null)
    {
        return View(new DuplicateElementViewModel{ OriginalElementName = elementName });
    }

    public async Task<IActionResult> ClassSourceCode(string elementName)
    {
        ViewBag.ClassSourceCode = await classGenerationService.GetClassSourceCode(elementName);
        ViewBag.ElementName = elementName;

        return PartialView("ClassSourceCode");
    }

    public async Task<IActionResult> Scripts(string elementName)
    {
        var formElement = await elementService.GetFormElementAsync(elementName);
        var scripts = await scriptsService.GetScriptsAsync(formElement);
        var tableExists = await entityRepository.TableExistsAsync(formElement.TableName);
        
        var model = new ElementScriptsViewModel
        {
            ElementName = elementName,
            Scripts = scripts,
            TableExists = tableExists
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddElementViewModel model)
    {
        var element = await elementService.CreateEntityAsync(model.Name, model.ImportFields);
        if (element != null)
        {
            return RedirectToAction("Index", "Entity", new { elementName = element.Name });
        }

        var jjValidationSummary = elementService.GetValidationSummary();
        ViewBag.Error = jjValidationSummary.GetHtml();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Duplicate(DuplicateElementViewModel model)
    {
        JJValidationSummary validationSummary;
        if (!ModelState.IsValid)
        {
            validationSummary = elementService.GetValidationSummary();
            ViewBag.Error = validationSummary.GetHtml();
        }
            
        if (await elementService.DuplicateEntityAsync(model.OriginalElementName!, model.NewElementName!))
            return RedirectToAction("Index", new { elementName = model.NewElementName });

        validationSummary = elementService.GetValidationSummary();
        ViewBag.Error = validationSummary.GetHtml();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Scripts(string elementName, string scriptOption)
    {
        try
        {
            await scriptsService.ExecuteScriptsAsync(elementName, scriptOption);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            var error = new { success = false, message = ex.Message };
            return new JsonResult(error) { StatusCode = (int)HttpStatusCode.InternalServerError };
        }
    }
    
    public async Task<IActionResult> Delete()
    {
        var formView = elementService.GetFormView();
        var selectedGridValues = formView.GridView.GetSelectedGridValues();
    
        var elementNamesToDelete = selectedGridValues
            .Where(value => value.TryGetValue("name", out var nameValue) && nameValue is string)
            .Select(value => value["name"].ToString())
            .ToList();

        foreach (var elementName in elementNamesToDelete)
        {
            await elementService.DataDictionaryRepository.DeleteAsync(elementName);
        }

        return RedirectToAction(nameof(Index));
    }

    
}
using JJMasterData.Commons.Exceptions;
using JJMasterData.Core.DataDictionary.Services;
using JJMasterData.Core.FormEvents.Args;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.Web.Components;
using JJMasterData.Web.Areas.DataDictionary.Models.ViewModels;
using JJMasterData.Web.Extensions;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Web.Areas.DataDictionary.Controllers;

public class ElementController : DataDictionaryController
{
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }
    private readonly ElementService _elementService;
    private readonly ClassGenerationService _classGenerationService;
    private readonly ScriptsService _scriptsService;

    public ElementController(
        ElementService elementService,
        ClassGenerationService classGenerationService,
        ScriptsService scriptsService,
        IStringLocalizer<JJMasterDataResources> stringLocalizer)
    {
        StringLocalizer = stringLocalizer;
        _elementService = elementService;
        _classGenerationService = classGenerationService;
        _scriptsService = scriptsService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            await _elementService.CreateStructureIfNotExistsAsync();
            
            var formView = _elementService.GetFormView();
            var result = await formView.GetResultAsync();
            
            if (result.IsActionResult())
                return result.ToActionResult();

            ViewBag.FormViewHtml = result.Content!;
            
            return View();
        }
        catch (DataAccessException)
        {
            return RedirectToAction("Index", "Options", new { Area = "DataDictionary", isFullscreen = true });
        }
    }

    public IActionResult Add()
    {
        return View();
    }

    public async Task<IActionResult> Export()
    {
        var formView =  _elementService.GetFormView();
        var selectedRows = formView.GridView.GetSelectedGridValues();

        if(selectedRows.Count == 1)
        {
            var jsonBytes =await  _elementService.ExportSingleRowAsync(selectedRows[0]);
            return File(jsonBytes, "application/json", selectedRows[0]["name"] + ".json");
        }

        var zipBytes = await _elementService.ExportMultipleRowsAsync(selectedRows);
        return File(zipBytes, "application/zip", "Dictionaries.zip");
    }

    public IActionResult Import()
    {
        return View(new ImportViewModel(ConfigureUploadArea));
    }

    private void ConfigureUploadArea(JJUploadArea upload)
    {
        upload.AddLabel = StringLocalizer["Select Dictionaries"];
        upload.AllowedTypes = "json";
        upload.AutoSubmitAfterUploadAll = false;
        upload.OnFileUploadedAsync += FileUploaded;
    }

    private async Task FileUploaded(object? sender, FormUploadFileEventArgs e)
    {
        await _elementService.Import(new MemoryStream(e.File.Bytes));
        if (ModelState.IsValid)
        {
            e.SuccessMessage = StringLocalizer["Dictionary imported successfully!"];
        }
        else
        {
            var jjSummary = _elementService.GetValidationSummary();
            foreach (var err in jjSummary.Errors)
                e.ErrorMessage += "<br>" + err;
        }
    }

    public IActionResult Duplicate(string dictionaryName)
    {
        return View(new { originName = dictionaryName });
    }

    public async Task<IActionResult> ClassSourceCode(string dictionaryName)
    {
        ViewBag.ClassSourceCode = await _classGenerationService.GetClassSourceCode(dictionaryName);
        ViewBag.DictionaryName = dictionaryName;

        return View("ClassSourceCode", "_MasterDataLayout.Popup");
    }

    public async Task<IActionResult> Scripts(string dictionaryName)
    {
        var scripts = await _scriptsService.GetScriptsListAsync(dictionaryName);

        var model = new ElementScriptsViewModel
        {
            DictionaryName = dictionaryName,
            CreateTableScript = scripts[0]!,
            WriteProcedureScript = scripts[1]!,
            ReadProcedureScript = scripts[2]!,
            AlterTableScript = scripts[3]
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Add(string tableName, bool importFields)
    {
        var element = await _elementService.CreateEntityAsync(tableName, importFields);
        if (element != null)
        {
            return RedirectToAction("Index", "Entity", new { dictionaryName = element.Name });
        }

        var jjValidationSummary = _elementService.GetValidationSummary();
        ViewBag.Error = jjValidationSummary.GetHtml();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Duplicate(string originName, string newName)
    {
        if (await _elementService.DuplicateEntityAsync(originName, newName))
        {
            return RedirectToAction("Index", new { dictionaryName = newName });
        }

        var jjValidationSummary = _elementService.GetValidationSummary();
        ViewBag.Error = jjValidationSummary.GetHtml();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Scripts(string dictionaryName, string scriptOption)
    {
        try
        {
            await _scriptsService.ExecuteScriptsAsync(dictionaryName, scriptOption);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            var error = new { success = false, message = ex.Message };
            return new JsonResult("error") { StatusCode = (int)HttpStatusCode.InternalServerError, Value = error };
        }
    }
    
    public async Task<IActionResult> Delete()
    {
        var formView = _elementService.GetFormView();
        var selectedGridValues = formView.GridView.GetSelectedGridValues();
    
        var elementNamesToDelete = selectedGridValues
            .Where(value => value.TryGetValue("name", out var nameValue) && nameValue is string)
            .Select(value => value["name"]?.ToString())
            .ToList();

        foreach (var elementName in elementNamesToDelete)
        {
            await _elementService.DataDictionaryRepository.DeleteAsync(elementName);
        }

        return RedirectToAction(nameof(Index));
    }

    
}
﻿using JJMasterData.Core.DataDictionary.Services;
using JJMasterData.Core.FormEvents.Abstractions;
using JJMasterData.Web.Areas.DataDictionary.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace JJMasterData.Web.Areas.DataDictionary.Controllers;

public class EntityController : DataDictionaryController
{
    private readonly EntityService _entityService;
    private readonly IFormEventResolver? _resolver;
    public EntityController(EntityService entityService, IFormEventResolver? resolver = null)
    {
        _entityService = entityService;
        _resolver = resolver;
    }

    public async Task<IActionResult> Index(string dictionaryName)
    {
        return View(await Populate(dictionaryName, true));
    }

    public async Task<IActionResult> Edit(string dictionaryName)
    {
        return View(await Populate(dictionaryName, false));
    }

    [HttpPost]        
    public async Task<ActionResult> Edit(
        EntityViewModel model)
    {
        var entity = await _entityService.EditEntityAsync(model.FormElement, model.DictionaryName);

        if (entity != null)
        {
            return RedirectToAction("Index", new { dictionaryName = entity.Name });
        }

        model.MenuId = "Entity";
        model.ValidationSummary = _entityService.GetValidationSummary();
            
        return View(model);

    }

    private async Task<EntityViewModel> Populate(string dictionaryName, bool readOnly)
    {
        var viewModel = new EntityViewModel(menuId:"Entity", dictionaryName:dictionaryName)
        {
            FormElement = await _entityService.GetFormElementAsync(dictionaryName),
            FormEvent = _resolver?.GetFormEvent(dictionaryName),
            ReadOnly = readOnly
        };

        return viewModel;
    }


}
﻿using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataDictionary.Services;
using JJMasterData.Web.Areas.DataDictionary.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace JJMasterData.Web.Areas.DataDictionary.Controllers;

public class RelationshipsController(RelationshipsService relationshipsService, IStringLocalizer<MasterDataResources> stringLocalizer)
    : DataDictionaryController
{
    #region Index

    public async Task<ActionResult> Index(string elementName)
    {
        var relationships = (await relationshipsService.GetFormElementAsync(elementName)).Relationships;

        var model = CreateListViewModel(elementName, relationships);

        return View(model);
    }

    [HttpPost]
    public async Task<ActionResult> Delete(string elementName, int id)
    {
        await relationshipsService.DeleteAsync(elementName, id);
        return RedirectToAction("Index", new { elementName });
    }

    [HttpPost]
    public async Task<ActionResult> Sort(string elementName, [FromBody] string[] relationships)
    {
        await relationshipsService.SortAsync(elementName, relationships);
        return Ok();
    }

    
    private static RelationshipsListViewModel CreateListViewModel(string elementName,
        FormElementRelationshipList relationships)
    {
        return new RelationshipsListViewModel(elementName, "Relationships")
        {
            Relationships = relationships
        };
    }

    #endregion

    #region ElementDetails

    public async Task<ActionResult> ElementDetails(string elementName, int? id)
    {
        var formElement = await relationshipsService.GetFormElementAsync(elementName);
        var relationship = id != null ? formElement.Relationships.GetById(id.Value).ElementRelationship! : new ElementRelationship();

        var model = await CreateElementDetailsViewModel(elementName, relationship, id);

        return View("DetailElement", model);
    }

    [HttpPost]
    public async Task<ActionResult> ElementDetails(RelationshipsElementDetailsViewModel model)
    {
        var newModel = await CreateElementDetailsViewModel(model.ElementName, model.Relationship, model.Id);
        return View("DetailElement", newModel);
    }

    [HttpPost]
    public async Task<ActionResult> CreateRelationship(RelationshipsElementDetailsViewModel model)
    {
        if (await relationshipsService.ValidateFinallyAddRelation(model.ElementName, model.Relationship,
                model.AddPrimaryKeyName!, model.AddForeignKeyName!))
        {
            model.Relationship.Columns.Add(new ElementRelationshipColumn(model.AddPrimaryKeyName!,
                model.AddForeignKeyName!));
        }

        await PopulateSelectLists(model);

        return View("DetailElement", model);
    }

    [HttpPost]
    public async Task<ActionResult> SaveRelationshipElement(RelationshipsElementDetailsViewModel model)
    {
        if (await relationshipsService.ValidateElementRelationship(model.Relationship, model.ElementName, model.Id))
        {
            await relationshipsService.SaveElementRelationship(model.Relationship, model.Id, model.ElementName);
            return Json(new { success = true });
        }

        await PopulateSelectLists(model);

        var jjSummary = relationshipsService.GetValidationSummary();
        return Json(new { success = false, errorMessage = jjSummary.GetHtml() });
    }

    [HttpPost]
    public async Task<ActionResult> DeleteRelationshipColumn(RelationshipsElementDetailsViewModel model, int columnIndex)
    {
        model.Relationship.Columns.RemoveAt(columnIndex);

        await PopulateSelectLists(model);

        return View("DetailElement", model);
    }

    private async Task PopulateSelectLists(RelationshipsElementDetailsViewModel model)
    {
        model.ElementsSelectList = await GetElementsSelectList(model.Relationship.ChildElement);
        model.ForeignKeysSelectList = await GetForeignKeysSelectList(model.Relationship.ChildElement);
        model.PrimaryKeysSelectList = await GetPrimaryKeysSelectList(model.ElementName);
    }

    private async Task<RelationshipsElementDetailsViewModel> CreateElementDetailsViewModel(
        string elementName,
        ElementRelationship relationship, int? id = null)
    {
        return new RelationshipsElementDetailsViewModel(elementName, "Relationships")
        {
            Id = id,
            Relationship = relationship,
            ElementsSelectList = await GetElementsSelectList(relationship.ChildElement),
            PrimaryKeysSelectList = await GetPrimaryKeysSelectList(elementName),
            ForeignKeysSelectList = await GetForeignKeysSelectList(relationship.ChildElement)
        };
    }

    public async Task<List<SelectListItem>> GetPrimaryKeysSelectList(string elementName)
    {
        var formElement = await relationshipsService.GetFormElementAsync(elementName);
        var selectList = formElement.Fields.Select(field => new SelectListItem(field.Name, field.Name)).ToList();

        return selectList;
    }

    private async Task<List<SelectListItem>> GetForeignKeysSelectList(string childElementName)
    {
        var selectList = new List<SelectListItem>();

        if (string.IsNullOrEmpty(childElementName))
        {
            selectList.Add(new SelectListItem(stringLocalizer["(Select)"], string.Empty));
        }
        else
        {
            var formElement = await relationshipsService.DataDictionaryRepository.GetFormElementAsync(childElementName);
            selectList.AddRange(formElement.Fields.Select(field => new SelectListItem(field.Name, field.Name)));
        }

        return selectList;
    }

    private async Task<List<SelectListItem>> GetElementsSelectList(string childElementName)
    {
        var list = await relationshipsService.DataDictionaryRepository.GetNameListAsync();

        var selectList = list.Select(name => new SelectListItem(name, name)).OrderBy(n=>n.Text).ToList();

        if (string.IsNullOrEmpty(childElementName))
        {
            selectList.Insert(0, new SelectListItem(stringLocalizer["(Select)"], string.Empty));
        }

        return selectList;
    }

    #endregion

    #region LayoutDetails

    [HttpGet]
    public async Task<IActionResult> LayoutDetails(string elementName, int id)
    {
        var model = await CreateLayoutDetailsViewModel(elementName, id);
        return View("DetailLayout", model);
    }
    
    [HttpPost]
    public IActionResult LayoutDetails(RelationshipsLayoutDetailsViewModel model)
    {
        return View("DetailLayout", model);
    }
    
    public async Task<IActionResult> SaveRelationshipLayout(RelationshipsLayoutDetailsViewModel model, FormElementPanel panel)
    {
        if (relationshipsService.ValidatePanel(panel))
        {
            await relationshipsService.SaveFormElementRelationship(panel,model.ViewType,model.EditModeOpenByDefault, model.Id, model.ElementName);
            return Json(new { success = true });
        }

        var jjSummary = relationshipsService.GetValidationSummary();
        return Json(new { success = false, errorMessage = jjSummary.GetHtml() });
    }

    private async Task<RelationshipsLayoutDetailsViewModel> CreateLayoutDetailsViewModel(
        string elementName,
        int id)
    {
        var formElement = await relationshipsService.DataDictionaryRepository.GetFormElementAsync(elementName);

        var relationship = formElement.Relationships.GetById(id);
        
        ViewBag.CodeMirrorHintList = JsonConvert.SerializeObject(BaseService.GetAutocompleteHintsList(formElement));
        
        return new RelationshipsLayoutDetailsViewModel(elementName, "Relationships")
        {
            Id = id,
            IsParent = relationship.IsParent,
            EditModeOpenByDefault = relationship.EditModeOpenByDefault,
            Panel = relationship.Panel,
            ViewType = relationship.ViewType
        };
    }
    
    #endregion
}
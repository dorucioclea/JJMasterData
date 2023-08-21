﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Extensions;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.DataDictionary.Services;

public class FieldService : BaseService
{


    public FieldService(
        IValidationDictionary validationDictionary, 
        IDataDictionaryRepository dataDictionaryRepository,
        IStringLocalizer<JJMasterDataResources> stringLocalizer)
        : base(validationDictionary, dataDictionaryRepository,stringLocalizer)
    {

    }

    public async Task<bool> SaveFieldAsync(string elementName, FormElementField field, string originalName)
    {
        var formElement = await DataDictionaryRepository.GetMetadataAsync(elementName);

        RemoveUnusedProperties(ref field);

        if (field.DataFile != null)
        {
            field.DataFile.MaxFileSize *= 1000000;
            field.DataFile.FolderPath = field.DataFile.FolderPath?.Trim();
        }

        if (!ValidateFields(formElement, field, originalName))
        {
            if (string.IsNullOrEmpty(field.Name))
                field.Name = originalName;

            return false;
        }

        if (formElement.Fields.Contains(originalName))
        {
            field.Actions = formElement.Fields[originalName].Actions;
            formElement.Fields[originalName] = field;
        }
        else
        {
            formElement.Fields.Add(field);
        }

        formElement.Fields[field.Name] = field;
        await DataDictionaryRepository.InsertOrReplaceAsync(formElement);

        return IsValid;
    }

    private void RemoveUnusedProperties(ref FormElementField field)
    {
        if (field.Component is FormComponent.ComboBox or FormComponent.Search or FormComponent.Lookup)
        {
            switch (field.DataItem!.DataItemType)
            {
                case DataItemType.Dictionary:
                    field.DataFile = null;
                    field.DataItem.Command = null;
                    field.DataItem.Items.Clear();
                    break;
                case DataItemType.Manual:
                    field.DataFile = null;
                    field.DataItem.Command = null;
                    field.DataItem.ElementMap = null;
                    break;
                case DataItemType.SqlCommand:
                    field.DataFile = null;
                    field.DataItem.ElementMap = null;
                    field.DataItem.Items.Clear();
                    break;
            }
        }
        else if (field.Component == FormComponent.File)
        {
            field.DataItem = null;
        }
        else
        {
            field.DataItem = null;
            field.DataFile = null;
        }
    }

    public bool ValidateFields(FormElement formElement, FormElementField field, string originalName)
    {
        ValidateName(field.Name);

        if (!string.IsNullOrEmpty(field.Name) && !field.Name.Equals(originalName))
        {
            if (formElement.Fields.Contains(field.Name))
                AddError(nameof(field.Name), StringLocalizer["Name of field already exists"]);
        }

        ValidateExpressions(field);

        if (field.DataType is FieldType.Varchar or FieldType.NVarchar)
        {
            if (field.Size <= 0)
                AddError(nameof(field.Size), StringLocalizer["Invalid [Size] field"]);
        }
        else
        {
            if (field.Filter.Type is FilterMode.MultValuesContain or FilterMode.MultValuesEqual)
            {
                AddError(nameof(field.Filter.Type),
                    StringLocalizer["MULTVALUES filters are only allowed for text type fields"]);
            }
        }

        if (field.AutoNum && field.DataType != FieldType.Int)
            AddError(nameof(field.AutoNum),
                StringLocalizer[
                    "Field with AutoNum (auto increment) must be of data type int, unencrypted and required"]);

        if (field.DataType != FieldType.Varchar && 
            field.DataType != FieldType.NVarchar && 
            field.DataType != FieldType.Text && 
            field.DataType != FieldType.NText)
        {
            if (field.Filter.Type is FilterMode.Contain)
            {
                AddError(nameof(field.Filter.Type),
                    StringLocalizer["Only fields of type VarChar or Text can be of type Contains."]);
            }
        }

        if (field.Component is FormComponent.Number or FormComponent.Currency)
        {
            if (field.NumberOfDecimalPlaces > 0)
            {
                if (field.DataType != FieldType.Float)
                {
                    AddError(nameof(field.DataType),
                        StringLocalizer["The field [NumberOfDecimalPlaces] cannot be defined with the type "] +
                        field.DataType);
                }

                if (field.IsPk)
                    AddError(nameof(field.DataType),
                        StringLocalizer["The primary key field must not contain [NumberOfDecimalPlaces]"]);
            }
            else
            {
                return IsValid;
            }
        }
        else if (field.Component is FormComponent.Lookup or FormComponent.ComboBox or FormComponent.Search)
        {
            ValidateDataItem(field.DataItem);
        }
        else if (field.Component == FormComponent.File)
        {
            ValidateDataFile(field.DataBehavior, field.DataFile);
        }

        return IsValid;
    }

    private void ValidateExpressions(FormElementField field)
    {
        if (string.IsNullOrWhiteSpace(field.VisibleExpression))
            AddError(nameof(field.VisibleExpression), StringLocalizer["Required [VisibleExpression] field"]);
        else if (!ValidateExpression(field.VisibleExpression, "val:", "exp:"))
            AddError(nameof(field.VisibleExpression), StringLocalizer["Invalid [VisibleExpression] field"]);

        if (string.IsNullOrWhiteSpace(field.EnableExpression))
            AddError(nameof(field.EnableExpression), StringLocalizer["Required [EnableExpression] field"]);
        else if (!ValidateExpression(field.EnableExpression, "val:", "exp:"))
            AddError(nameof(field.EnableExpression), StringLocalizer["Invalid [EnableExpression] field"]);

        if (!string.IsNullOrEmpty(field.DefaultValue))
        {
            if (!ValidateExpression(field.DefaultValue, "val:", "exp:", "sql:", "protheus:"))
                AddError(nameof(field.DefaultValue), StringLocalizer["Invalid [DefaultValue] field"]);
        }

        if (!string.IsNullOrEmpty(field.TriggerExpression))
        {
            if (!ValidateExpression(field.TriggerExpression, "val:", "exp:", "sql:", "protheus:"))
                AddError(nameof(field.TriggerExpression), StringLocalizer["Invalid [TriggerExpression] field"]);
        }
    }

    private void ValidateDataItem(FormElementDataItem data)
    {
        if (data == null)
        {
            AddError("DataItem", StringLocalizer["Undefined font settings"]);
        }

        switch (data!.DataItemType)
        {
            case DataItemType.SqlCommand:
            {
                if (string.IsNullOrEmpty(data.Command.Sql))
                    AddError("Command.Sql", StringLocalizer["[Field Command.Sql] required"]);

                if (data.ReplaceTextOnGrid && !data.Command!.Sql!.Contains("{search_id}"))
                {
                    AddError("Command.Sql", "{search_id} is required at queries using ReplaceTextOnGrid. " +
                                            "Check <a href=\"https://portal.jjconsulting.com.br/jjdoc/articles/errors/jj002.html\">JJ002</a> for more information.");
                }

                break;
            }
            case DataItemType.Manual:
                ValidateManualItens(data.Items);
                break;
            case DataItemType.Dictionary:
                ValidateDataElementMap(data.ElementMap);
                break;
        }
    }

    private void ValidateManualItens(IList<DataItemValue> itens)
    {
        if (itens == null || itens.Count == 0)
        {
            AddError("DataItem", StringLocalizer["Item list not defined"]);
        }

        if (itens != null)
            for (int i = 0; i < itens.Count; i++)
            {
                var it = itens[i];
                if (string.IsNullOrEmpty(it.Id))
                    AddError("DataItem", StringLocalizer["Item id {0} required", i]);

                if (string.IsNullOrEmpty(it.Description))
                    AddError("DataItem", StringLocalizer["Item description {0} required", i]);
            }
    }

    private void ValidateDataElementMap(DataElementMap data)
    {
        if (data == null)
        {
            AddError("ElementMap", StringLocalizer["Undefined mapping settings"]);
        }

        if (string.IsNullOrEmpty(data!.ElementName))
            AddError(nameof(data.ElementName), StringLocalizer["Required field [ElementName]"]);
    }

    private void ValidateDataFile(FieldBehavior dataBehavior, FormElementDataFile dataFile)
    {
        if (dataFile == null)
        {
            AddError("DataFile", StringLocalizer["Undefined file settings"]);
        }

        if (dataBehavior == FieldBehavior.Virtual)
            AddError("DataFile", StringLocalizer["Fields of type FILE cannot be virtual"]);

        if (string.IsNullOrEmpty(dataFile?.FolderPath))
            AddError(nameof(dataFile.FolderPath), StringLocalizer["Field [{nameof(dataFile.FolderPath)}] required"]);

        if (string.IsNullOrEmpty(dataFile?.AllowedTypes))
            AddError(nameof(dataFile.AllowedTypes), StringLocalizer["Required [AllowedTypes] field"]);

        if (dataFile!.MultipleFile & dataFile.ExportAsLink)
            AddError(nameof(dataFile.ExportAsLink),
                StringLocalizer["The [ExportAsLink] field cannot be enabled with [MultipleFile]"]);
    }

    public async Task<bool> SortFieldsAsync(string elementName, string[] orderFields)
    {
        var formElement = await DataDictionaryRepository.GetMetadataAsync(elementName);
        var newList = orderFields.Select(fieldName => formElement.Fields[fieldName]).ToList();

        for (int i = 0; i < formElement.Fields.Count; i++)
        {
            formElement.Fields[i] = newList[i];
        }
        
        await DataDictionaryRepository.InsertOrReplaceAsync(formElement);
        return true;
    }

    public async Task<bool> AddElementMapFilterAsync(FormElementField field, DataElementMapFilter mapFilter)
    {
        var elementMap = field.DataItem!.ElementMap;

        if (string.IsNullOrEmpty(mapFilter.FieldName))
            AddError(nameof(mapFilter.FieldName), StringLocalizer["Required filter field"]);

        if (!string.IsNullOrEmpty(mapFilter.ExpressionValue) &&
            !mapFilter.ExpressionValue.Contains("val:") &&
            !mapFilter.ExpressionValue.Contains("exp:") &&
            !mapFilter.ExpressionValue.Contains("sql:") &&
            !mapFilter.ExpressionValue.Contains("protheus:"))
        {
            AddError(nameof(mapFilter.ExpressionValue), StringLocalizer["Invalid filter field"]);
        }

        if (string.IsNullOrEmpty(elementMap.FieldKey))
            AddError(nameof(elementMap.FieldKey),
                StringLocalizer["Required [{0}] field", StringLocalizer["Field Key"]]);

        if (string.IsNullOrEmpty(elementMap.ElementName))
            AddError(nameof(elementMap.ElementName), StringLocalizer["Required [{0}] field", StringLocalizer["Field Key"]]);

        if (IsValid)
        {
            var dataEntry = await DataDictionaryRepository.GetMetadataAsync(elementMap.ElementName);
            var fieldKey = dataEntry.Fields[elementMap.FieldKey];
            if (!fieldKey.IsPk & fieldKey.Filter.Type == FilterMode.None)
            {
                string err = StringLocalizer["Field [{0}] invalid, as it is not PK or not configured as a filter",
                    elementMap.FieldKey];
                AddError(nameof(elementMap.FieldKey), err);
            }
        }

        if (IsValid)
        {
            field.DataItem.ElementMap.MapFilters.Add(mapFilter);
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteField(string dictionaryName, string fieldName)
    {
        var formElement = await DataDictionaryRepository.GetMetadataAsync(dictionaryName);
        if (!formElement.Fields.Contains(fieldName))
            return false;
        
        var field = formElement.Fields[fieldName];
        formElement.Fields.Remove(field);
        await DataDictionaryRepository.InsertOrReplaceAsync(formElement);

        return IsValid;
    }

    public async Task<string> GetNextFieldNameAsync(string dictionaryName, string fieldName)
    {
        var formElement = await DataDictionaryRepository.GetMetadataAsync(dictionaryName);
        string nextField = null;
        if (formElement.Fields.Contains(fieldName))
        {
            var currentField = formElement.Fields[fieldName];
            int iIndex = formElement.Fields.IndexOf(currentField.Name);
            if (iIndex >= 0 && iIndex < formElement.Fields.Count - 1)
            {
                nextField = formElement.Fields[iIndex + 1].Name;
            }
        }

        return nextField;
    }

    public async Task<Dictionary<string, string>> GetElementFieldListAsync(FormElementField currentField)
    {
        var dicFields = new Dictionary<string, string>();
        dicFields.Add(string.Empty, StringLocalizer["--Select--"]);

        var map = currentField.DataItem!.ElementMap;
        if (string.IsNullOrEmpty(map.ElementName))
            return dicFields;

        var dataEntry = await DataDictionaryRepository.GetMetadataAsync(map.ElementName);
        if (dataEntry == null)
            return dicFields;

        foreach (var field in dataEntry.Fields)
        {
            dicFields.Add(field.Name, field.Name);
        }

        return dicFields;
    }

    public async Task<bool> CopyFieldAsync(FormElement formElement, FormElementField field)
    {
        var newField = field.DeepCopy();

        if (formElement.Fields.Contains(newField.Name))
        {
            AddError(newField.Name, StringLocalizer["Name of field already exists"]);
            return IsValid;
        }

        formElement.Fields.Add(newField);
        await DataDictionaryRepository.InsertOrReplaceAsync(formElement);
        return IsValid;
    }
}
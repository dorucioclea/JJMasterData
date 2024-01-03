﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Util;
using JJMasterData.Commons.Validations;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.UI.Components;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.DataDictionary.Services;

public abstract class BaseService(IValidationDictionary validationDictionary, 
    IDataDictionaryRepository dataDictionaryRepository,
    IStringLocalizer<MasterDataResources> stringLocalizer)
{
    public IDataDictionaryRepository DataDictionaryRepository { get; } = dataDictionaryRepository;
    protected IStringLocalizer<MasterDataResources> StringLocalizer { get; } = stringLocalizer;

    protected void AddError(string field, string message)
    {
        validationDictionary?.AddError(field, message);
    }
    
    protected void RemoveError(string field)
    {
        validationDictionary?.RemoveError(field);
    }
    
    public JJValidationSummary GetValidationSummary()
    {
        var factory = new ValidationSummaryFactory(StringLocalizer);
        return factory.Create(validationDictionary.Errors.ToList());
    }

    public bool IsValid => validationDictionary.IsValid;

    public Task<FormElement> GetFormElementAsync(string elementName)
    {
        return DataDictionaryRepository.GetFormElementAsync(elementName);
    }

    public bool ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            AddError(nameof(name), StringLocalizer["Required [Name] field"]);
            return false;
        }

        if (Validate.IsMasterDataKeyword(name))
            AddError(nameof(name), StringLocalizer["The [Name] field contains a reserved word used in the api"]);

        if (name.Contains(" "))
            AddError(nameof(name), StringLocalizer["The [Name] field cannot contain blank spaces."]);

        if (name.Length > 64)
            AddError(nameof(name), StringLocalizer["The [Name] field cannot contain more than 64 characters."]);

        string[] chars = ["&", "?", "=", ",", "'", "[", "]", "/", "\\", "+", "!", " "];
        foreach (string c in chars)
        {
            if (name.Contains(c))
                AddError(nameof(name), StringLocalizer[$"The [Name] field contains an invalid character({c})"]);
        }

        string nameNoAccents = StringManager.GetStringWithoutAccents(name);
        if (!nameNoAccents.Equals(name))
            AddError(nameof(name), StringLocalizer["The [Name] field cannot contain accents."]);

        if (Validate.IsDatabaseKeyword(name))
            AddError(nameof(name), StringLocalizer["The [Name] field contains a reserved word used in the database."]);


        return validationDictionary.IsValid;
    }

    protected static bool ValidateExpression(string value, params string[] args)
    {
        return args.Any(value.StartsWith);
    }

    public IEnumerable<string> GetAutocompleteHintsList(FormElement formElement, bool includeAdditionalHints = true)
    {
        if (includeAdditionalHints)
        {
            yield return "PageState";
            yield return "FieldName";
            yield return "UserId";
            yield return "SearchId";
            yield return "SearchText";
        }
        
        foreach (var field in formElement.Fields)
            yield return field.Name;
    }

    public async Task<Dictionary<string, string>> GetElementListAsync()
    {
        var dicElement = new Dictionary<string, string> { { string.Empty, StringLocalizer["--Select--"] } };

        var list = DataDictionaryRepository.GetNameListAsync();
        
        await foreach (string name in list)
        {
            dicElement.Add(name, name);
        }

        return dicElement;
    }
}
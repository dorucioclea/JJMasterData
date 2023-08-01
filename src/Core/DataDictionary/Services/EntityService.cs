﻿using System;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.DataDictionary.Services;

public class EntityService : BaseService
{


    public EntityService(
        IValidationDictionary validationDictionary,
        IDataDictionaryRepository dataDictionaryRepository,
        IStringLocalizer<JJMasterDataResources> stringLocalizer)
        : base(validationDictionary, dataDictionaryRepository,stringLocalizer)
    {

    }

    private bool ValidateEntity(Element formElement, string originName)
    {
        if (ValidateName(formElement.Name) && !originName.ToLower().Equals(formElement.Name.ToLower()))
        {
            if (DataDictionaryRepository.Exists(formElement.Name))
                AddError("Name", StringLocalizer["There is already a dictionary with the name {0}",formElement.Name]);
        }

        if (string.IsNullOrEmpty(formElement.TableName))
            AddError("TableName", StringLocalizer["Required table name field"]);

    
        if (!string.IsNullOrEmpty(formElement.CustomProcNameGet) &&
            !string.IsNullOrEmpty(formElement.CustomProcNameSet))
        { 
            if (formElement.CustomProcNameGet.ToLower().Equals(formElement.CustomProcNameSet.ToLower()))
            {
                AddError("CustomProcNameGet", StringLocalizer["Procedure names cannot be identical"]);
            }
        }
                

        return IsValid;
    }


    public async Task<FormElement> EditEntityAsync(FormElement formElement, string entityName)
    {
        if (!ValidateEntity(formElement, entityName))
            return null;
        
        try
        {
            var dicParser = await DataDictionaryRepository.GetMetadataAsync(entityName);

            dicParser.Name = formElement.Name;
            dicParser.TableName = formElement.TableName;
            dicParser.CustomProcNameGet = formElement.CustomProcNameGet;
            dicParser.CustomProcNameSet = formElement.CustomProcNameSet;
            dicParser.Info = formElement.Info;
            dicParser.Title = formElement.Title;
            dicParser.SubTitle = formElement.SubTitle;

            if (!entityName.Equals(formElement.Name))
            {
                DataDictionaryRepository.Delete(entityName);
                DataDictionaryRepository.InsertOrReplace(dicParser);
            }
            else
            {
                DataDictionaryRepository.InsertOrReplace(dicParser);
            }

            return formElement;

        }
        catch (Exception ex)
        {
            AddError("Entity", ex.Message);
            return null;
        }

    }


}
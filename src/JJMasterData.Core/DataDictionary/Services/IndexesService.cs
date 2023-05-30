﻿using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataDictionary.Services.Abstractions;

namespace JJMasterData.Core.DataDictionary.Services;

public class IndexesService : BaseService
{
    public IndexesService(IValidationDictionary validationDictionary, IDataDictionaryRepository dataDictionaryRepository)
        : base(validationDictionary, dataDictionaryRepository)
    {
    }

    public bool Save(string id, string index, ElementIndex elementIndex)
    {
        var formElement = DataDictionaryRepository.GetMetadata(id);

        
        if (!string.IsNullOrEmpty(index))
        {
            formElement.Indexes[int.Parse(index)] = elementIndex;
        }
        else
        {
            formElement.Indexes.Add(elementIndex);
        }

        if (Validate(elementIndex))
        {
            DataDictionaryRepository.InsertOrReplace(formElement);
        }

        return IsValid;

    }

    public bool Validate(ElementIndex elementIndext)
    {
        if (elementIndext.Columns == null || elementIndext.Columns.Count == 0)
            AddError("", Translate.Key("Select a field to compose the index"));

        return IsValid;
    }

    public void Delete(string dictionaryName, string index)
    {
        var dictionary = DataDictionaryRepository.GetMetadata(dictionaryName);
        var elementIndex = dictionary.Indexes[int.Parse(index)];
        dictionary.Indexes.Remove(elementIndex);
        DataDictionaryRepository.InsertOrReplace(dictionary);
    }

    public void MoveDown(string dictionaryName, string index)
    {
        var dictionary = DataDictionaryRepository.GetMetadata(dictionaryName);
        var indexes = dictionary.Indexes;
        int indexToMoveDown = int.Parse(index);
        if (indexToMoveDown >= 0 && indexToMoveDown < indexes.Count - 1)
        {
            (indexes[indexToMoveDown + 1], indexes[indexToMoveDown]) = (indexes[indexToMoveDown], indexes[indexToMoveDown + 1]);
            DataDictionaryRepository.InsertOrReplace(dictionary);
        }
    }

    public void MoveUp(string dictionaryName, string index)
    {
        var dictionary = DataDictionaryRepository.GetMetadata(dictionaryName);
        var indexes = dictionary.Indexes;
        int indexToMoveUp = int.Parse(index);
        if (indexToMoveUp > 0)
        {
            (indexes[indexToMoveUp - 1], indexes[indexToMoveUp]) = (indexes[indexToMoveUp], indexes[indexToMoveUp - 1]);
            DataDictionaryRepository.InsertOrReplace(dictionary);
        }
    }

}
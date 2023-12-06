﻿#nullable enable
using JJMasterData.Commons.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Commons.Data.Entity.Repository;
using JJMasterData.Commons.Data.Entity.Repository.Abstractions;
using JJMasterData.Core.Configuration.Options;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataDictionary.Structure;
using Microsoft.Extensions.Options;

namespace JJMasterData.Core.DataDictionary.Repository;

public class SqlDataDictionaryRepository(IEntityRepository entityRepository, IOptions<MasterDataCoreOptions> options)
    : IDataDictionaryRepository
{
    internal Element MasterDataElement { get; } = DataDictionaryStructure.GetElement(options.Value.DataDictionaryTableName);

    public async Task<IEnumerable<FormElement>> GetFormElementListAsync(bool? apiSync = null)
    {
        var filters = new Dictionary<string, object?>();
        if (apiSync.HasValue)
            filters.Add(DataDictionaryStructure.EnableSynchronism, apiSync);

        filters[DataDictionaryStructure.Type] = "F";

        var orderBy = new OrderByData();
        orderBy.AddOrReplace(DataDictionaryStructure.Name, OrderByDirection.Asc);
        orderBy.AddOrReplace(DataDictionaryStructure.Type, OrderByDirection.Asc);

        var result = await entityRepository.GetDictionaryListResultAsync(MasterDataElement,
            new EntityParameters { Filters = filters, OrderBy = orderBy }, false);

        return ParseDictionaryList(result.Data);
    }

    private static IEnumerable<FormElement> ParseDictionaryList(IEnumerable<Dictionary<string, object?>> result)
    {
        foreach (var row in result)
        {
            yield return FormElementSerializer.Deserialize(row[DataDictionaryStructure.Json]!.ToString()!);
        }
    }

    public async IAsyncEnumerable<string> GetNameListAsync()
    {
        var filter = new Dictionary<string, object?> { { DataDictionaryStructure.Type, "F" } };

        var dt = await entityRepository.GetDictionaryListResultAsync(MasterDataElement,
            new EntityParameters { Filters = filter }, false);
        foreach (var row in dt.Data)
        {
            yield return row[DataDictionaryStructure.Name]!.ToString()!;
        }
    }


    public FormElement? GetFormElement(string elementName)
    {
        var filter = new Dictionary<string, object> { { DataDictionaryStructure.Name, elementName }, {DataDictionaryStructure.Type, "F" } };

        var values =  entityRepository.GetFields(MasterDataElement, filter);

        var model = values.ToModel<DataDictionaryModel>();

        return model != null ? FormElementSerializer.Deserialize(model.Json) : null;
    }

    public async Task<FormElement?> GetFormElementAsync(string elementName)
    {
        var filter = new Dictionary<string, object> { { DataDictionaryStructure.Name, elementName }, {DataDictionaryStructure.Type, "F" } };

        var values = await entityRepository.GetFieldsAsync(MasterDataElement, filter);

        var model = values.ToModel<DataDictionaryModel>();

        return model != null ? FormElementSerializer.Deserialize(model.Json) : null;
    }


    public async Task InsertOrReplaceAsync(FormElement formElement)
    {
        var values = GetFormElementDictionary(formElement);

        await entityRepository.SetValuesAsync(MasterDataElement, values);
    }

    public void InsertOrReplace(FormElement formElement)
    {
        var values = GetFormElementDictionary(formElement);

        entityRepository.SetValues(MasterDataElement, values);
    }

    private static Dictionary<string, object?> GetFormElementDictionary(FormElement formElement)
    {
        if (formElement == null)
            throw new ArgumentNullException(nameof(formElement));

        if (formElement == null)
            throw new ArgumentNullException(nameof(formElement));

        if (string.IsNullOrEmpty(formElement.Name))
            throw new ArgumentNullException(nameof(formElement.Name));

        var name = formElement.Name;

        var dNow = DateTime.Now;

        var jsonForm = FormElementSerializer.Serialize(formElement);

        var values = new Dictionary<string, object?>
        {
            { DataDictionaryStructure.Name, name },
            { DataDictionaryStructure.TableName, formElement.TableName },
            { DataDictionaryStructure.Info, formElement.Info },
            { DataDictionaryStructure.Type, "F" },
            { DataDictionaryStructure.Owner, null },
            { DataDictionaryStructure.Json, jsonForm },
            { DataDictionaryStructure.EnableSynchronism, formElement.EnableSynchronism },
            { DataDictionaryStructure.LastModified, dNow }
        };

        return values;
    }

    public async Task DeleteAsync(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            throw new ArgumentException();

        var filters = new Dictionary<string, object> { { DataDictionaryStructure.Name, elementName } };

        await entityRepository.DeleteAsync(MasterDataElement, filters);
    }


    public async Task<bool> ExistsAsync(string elementName)
    {
        var filter = new Dictionary<string, object> { { DataDictionaryStructure.Name, elementName } };
        var fields = await entityRepository.GetFieldsAsync(MasterDataElement, filter);
        return fields.Any();
    }

    public async Task CreateStructureIfNotExistsAsync()
    {
        if (!await entityRepository.TableExistsAsync(MasterDataElement.Name))
            await entityRepository.CreateDataModelAsync(MasterDataElement);
    }

    public async Task<ListResult<FormElementInfo>> GetFormElementInfoListAsync(DataDictionaryFilter filter,
        OrderByData orderBy, int recordsPerPage, int currentPage)
    {
        var filters = filter.ToDictionary();
        filters.Add(DataDictionaryStructure.Type, "F");

        var result = await entityRepository.GetDictionaryListResultAsync(MasterDataElement,
            new EntityParameters
            {
                Filters = filters!, OrderBy = orderBy, CurrentPage = currentPage, RecordsPerPage = recordsPerPage
            });

        var formElementInfoList = result.Data.Select(FormElementInfo.FromDictionary).ToList();

        return new ListResult<FormElementInfo>(formElementInfoList, result.TotalOfRecords);
    }
}
#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity.Repository;
using JJMasterData.Commons.Data.Entity.Repository.Abstractions;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager.Expressions;
using JJMasterData.Core.DataManager.Models;

namespace JJMasterData.Core.DataManager.Services;

public class ElementMapService(IDataDictionaryRepository dataDictionaryRepository,IEntityRepository entityRepository,ExpressionsService expressionsService)
{
    private IDataDictionaryRepository DataDictionaryRepository { get; } = dataDictionaryRepository;
    private IEntityRepository EntityRepository { get; } = entityRepository;
    private ExpressionsService ExpressionsService { get; } = expressionsService;

    public async Task<IDictionary<string, object?>> GetFieldsAsync(DataElementMap elementMap, object? value, FormStateData formStateData)
    {
        var formElement = await DataDictionaryRepository.GetFormElementAsync(elementMap.ElementName);
        var filters = GetFilters(elementMap, value, formStateData);
        return await EntityRepository.GetFieldsAsync(formElement, filters);
    }
    
    public async Task<List<Dictionary<string, object?>>> GetDictionaryList(DataElementMap elementMap, object? value, FormStateData formStateData)
    {
        var formElement = await DataDictionaryRepository.GetFormElementAsync(elementMap.ElementName);
        var filters = GetFilters(elementMap, value, formStateData);
        return await EntityRepository.GetDictionaryListAsync(formElement, new EntityParameters()
        {
            Filters = filters!
        });
    }
    
    private IDictionary<string, object> GetFilters(DataElementMap elementMap, object? value, FormStateData formStateData)
    {
        var filters = new Dictionary<string, object>();

        if (elementMap.Filters.Count > 0)
        {
            foreach (var filter in elementMap.Filters)
            {
                var filterParsed =
                    ExpressionsService.ReplaceExpressionWithParsedValues(filter.Value.ToString(), formStateData) ?? string.Empty;
                filters[filter.Key] = filterParsed;
            }
        }
        else
        {
            filters[elementMap.FieldId] = value?.ToString()!;
        }
       
        return filters;
    }
}
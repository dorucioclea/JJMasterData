#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Data;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager.Expressions.Abstractions;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.Http.Abstractions;
using JJMasterData.Core.Web.Components;

namespace JJMasterData.Core.DataManager.Services;

public class DataItemService : IDataItemService
{
    private IEntityRepository EntityRepository { get; }
    private IExpressionsService ExpressionsService { get; }
    private IFormValues FormValues { get; }
    private ElementMapService ElementMapService { get; }

    public DataItemService(
        IEntityRepository entityRepository,
        IExpressionsService expressionsService,
        IFormValues formValues,
        ElementMapService elementMapService)
    {
        EntityRepository = entityRepository;
        ExpressionsService = expressionsService;
        FormValues = formValues;
        ElementMapService = elementMapService;
    }

    public async Task<string?> GetSelectedValueAsync(FormElementField field, FormStateData formStateData,
        string? searchText = null, string? searchId = null)
    {
        var value = FormValues[field.Name];
        if (value is not null)
            return value;

        var first = await GetValuesAsync(field.DataItem!, formStateData, searchText, searchId).FirstOrDefaultAsync();

        return first?.Id;
    }

    public IEnumerable<DataItemResult> GetItems(FormElementDataItem dataItem, IEnumerable<DataItemValue> values)
    {
        foreach (var i in values.ToArray())
        {
            var description = dataItem.ShowIcon
                ? $"{i.Description}|{i.Icon.GetCssClass()}|{i.IconColor}"
                : i.Description;

            yield return new DataItemResult(i.Id, description);
        }
    }

    public async IAsyncEnumerable<DataItemValue> GetValuesAsync(
        FormElementDataItem dataItem,
        FormStateData formStateData,
        string? searchText,
        string? searchId)
    {
        switch (dataItem.DataItemType)
        {
            case DataItemType.Manual:
            {
                foreach (var item in dataItem.Items!)
                    yield return item;
                yield break;
            }
            case DataItemType.SqlCommand:
                await foreach (var value in GetSqlCommandValues(dataItem, formStateData,searchId, searchText))
                    yield return value;
                yield break;
            case DataItemType.ElementMap:
                await foreach (var value in GetElementMapValues(dataItem, formStateData, searchId,searchText))
                    yield return value;
                yield break;
        }
    }

    private async IAsyncEnumerable<DataItemValue> GetElementMapValues(FormElementDataItem dataItem, FormStateData formStateData, string? searchId, string? searchText)
    {
        var elementMap = dataItem.ElementMap;
        var values = await ElementMapService.GetDictionaryList(elementMap!, searchId, formStateData);
            
        foreach(var value in values)
        {
            var item = new DataItemValue
            {
                Id = value[elementMap!.FieldId]?.ToString(),
                Description = value[elementMap.FieldDescription]?.ToString(),
            };
            if (dataItem.ShowIcon)
            {
                if (elementMap.FieldIconId != null)
                    item.Icon = (IconType)int.Parse(value[elementMap.FieldIconId]?.ToString() ?? string.Empty);
                
                if (elementMap.FieldIconColor != null)
                    item.IconColor = value[elementMap.FieldIconColor]?.ToString();
            }

            if (searchText == null || item.Description!.ToLower().Contains(searchText))
            {
                yield return item;
            }
        }
    }

    private async IAsyncEnumerable<DataItemValue> GetSqlCommandValues(FormElementDataItem dataItem,
        FormStateData formStateData,
        string? searchId,
        string? searchText)
    {
        var sql = GetSqlParsed(dataItem, formStateData, searchText, searchId);

        var dictionary = await EntityRepository.GetDictionaryListAsync(new DataAccessCommand(sql!));

        foreach (var row in dictionary)
        {
            var item = new DataItemValue
            {
                Id = row.ElementAt(0).Value?.ToString(),
                Description = row.ElementAt(1).Value?.ToString()!.Trim()
            };
            if (dataItem.ShowIcon)
            {
                item.Icon = (IconType)int.Parse(row.ElementAt(2).Value?.ToString() ?? string.Empty);
                item.IconColor = row.ElementAt(3).Value?.ToString();
            }

            if (searchText == null || item.Description!.ToLower().Contains(searchText))
            {
                yield return item;
            }
        }
    }

    private string? GetSqlParsed(FormElementDataItem dataItem, FormStateData formStateData, string? searchText,
        string? searchId)
    {
        var sql = dataItem.Command!.Sql;
        if (sql.Contains("{"))
        {
            if (searchId != null)
            {
                if (formStateData.UserValues != null && !formStateData.UserValues.ContainsKey("search_id"))
                    formStateData.UserValues.Add("search_id", StringManager.ClearText(searchId));
            }

            if (searchText != null)
            {
                if (formStateData.UserValues != null && !formStateData.UserValues.ContainsKey("search_text"))
                    formStateData.UserValues.Add("search_text", StringManager.ClearText(searchText));
            }

            sql = ExpressionsService.ParseExpression(sql, formStateData, false);
        }

        return sql;
    }
}
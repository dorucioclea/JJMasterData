#nullable enable
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.Web.Components;

namespace JJMasterData.Core.DataManager.Services;

public class SearchBoxService : ISearchBoxService
{
    private IEntityRepository EntityRepository { get; }
    private IExpressionsService ExpressionsService { get; }

    public SearchBoxService(IEntityRepository entityRepository, IExpressionsService expressionsService)
    {
        EntityRepository = entityRepository;
        ExpressionsService = expressionsService;
    }

    public IEnumerable<SearchBoxItem> GetSearchBoxItems(FormElementDataItem dataItem, IEnumerable<DataItemValue> values)
    {
        foreach (var i in values.ToArray())
        {
            string description;
            
            if (dataItem.ShowImageLegend)
                description = $"{i.Description}|{i.Icon.GetCssClass()}|{i.ImageColor}";
            else
                description = i.Description;
            
            yield return new SearchBoxItem(i.Id, description);
        }
    }

    public async Task<IEnumerable<DataItemValue>> GetValues(FormElementDataItem dataItem,
        string? searchText,
        string? searchId,
        SearchBoxContext searchBoxContext)
    {
        if (dataItem.Command == null || string.IsNullOrEmpty(dataItem.Command.Sql))
            return dataItem.Items;

        var values = new List<DataItemValue>();
        var sql = GetSqlParsed(dataItem, searchText, searchId, searchBoxContext);

        var dt = await EntityRepository.GetDataTableAsync(sql);
        foreach (DataRow row in dt.Rows)
        {
            var item = new DataItemValue
            {
                Id = row[0].ToString(),
                Description = row[1].ToString()?.Trim()
            };
            if (dataItem.ShowImageLegend)
            {
                item.Icon = (IconType)int.Parse(row[2].ToString() ?? string.Empty);
                item.ImageColor = row[3].ToString();
            }

            values.Add(item);
        }

        return searchText != null ? values.Where(v=>v.Description.ToLower().Contains(searchText)) : values;
    }

    private string GetSqlParsed(FormElementDataItem dataItem, string? text, string? searchId, SearchBoxContext searchBoxContext)
    {
        var ( values, userValues, pageState) = searchBoxContext;

        string sql = dataItem.Command.Sql;
        if (sql.Contains("{"))
        {
            if (searchId != null)
            {
                if (searchBoxContext.UserValues != null && !searchBoxContext.UserValues.ContainsKey("search_id"))
                    searchBoxContext.UserValues.Add("search_id", StringManager.ClearText(searchId));
            }

            if (text != null)
            {
                if (searchBoxContext.UserValues != null && !searchBoxContext.UserValues.ContainsKey("search_text"))
                    searchBoxContext.UserValues.Add("search_text", StringManager.ClearText(text));
            }

            sql = ExpressionsService.ParseExpression(sql, pageState, false,
                values, userValues);
        }

        return sql;
    }
}
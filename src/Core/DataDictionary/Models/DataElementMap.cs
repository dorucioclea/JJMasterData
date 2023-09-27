﻿#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JJMasterData.Core.DataDictionary;


public class DataElementMap
{
    [JsonProperty("elementName")] 
    public string ElementName { get; set; } = null!;

    [JsonProperty("fieldKey")]
    public string FieldId { get; set; } = null!;

    [JsonProperty("fieldDescription")]
    public string FieldDescription { get; set; } = null!;

    [JsonProperty("iconId")] 
    public string? FieldIconId { get; set; }
    
    [JsonProperty("iconColor")]
    public string? FieldIconColor { get; set; }
    
    [JsonProperty("popUpSize")]
    public ModalSize ModalSize { get; set; }

    public IDictionary<string, object> Filters 
    {
        get
        {
            var filters = new Dictionary<string, object>();
            
            if (MapFilters != null)
            {
                foreach (var item in MapFilters)
                    filters.Add(item.FieldName, item.ExpressionValue);
            }

                
            return filters;
        }
        set
        {
            MapFilters.Clear();
            foreach (var s in value)
            {
                var mapFilter = new DataElementMapFilter
                {
                    FieldName = s.Key,
                    ExpressionValue = s.ToString()
                };
                MapFilters.Add(mapFilter);
            }
        }
    }

    [JsonProperty("mapFilters")]
    public List<DataElementMapFilter> MapFilters { get; set; } = new();


    [JsonProperty("enableElementActions")]
    public bool EnableElementActions { get; set; }
}
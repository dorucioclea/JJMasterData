﻿#nullable disable
using System.Runtime.Serialization;
using JJMasterData.Core.DataDictionary;
using Newtonsoft.Json;

namespace JJMasterData.FormElementUpdater.Models;


public class MetadataOptions
{
    [JsonProperty("grid")]
    public GridUI Grid { get; set; }

    [JsonProperty("form")]
    public FormUI Form { get; set; }

    [JsonProperty("toolBarActions")]
    public GridToolbarActions ToolbarActions { get; set; }

    [JsonProperty("gridActions")]
    public GridActions GridActions { get; set; }

    public MetadataOptions()
    {
        Grid = new GridUI();
        Form = new FormUI();
        ToolbarActions = new GridToolbarActions();
        GridActions = new GridActions();
    }
}
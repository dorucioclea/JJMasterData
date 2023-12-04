﻿#nullable disable

using System.Runtime.Serialization;
using JJMasterData.Core.DataDictionary.Models;
using Newtonsoft.Json;

namespace JJMasterData.ConsoleApp.Models.FormElementMigration;

[DataContract]
public class MetadataForm
{
    [JsonProperty("formfields")]
    public List<MetadataFormField> FormFields { get; set; } = new();

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("subtitle")]
    public string SubTitle { get; set; }

    [JsonProperty("panels")]
    public List<FormElementPanel> Panels { get; set; } = new();
}
﻿using System;
using System.Runtime.Serialization;

namespace JJMasterData.Core.Web.Components;

/// <summary>
/// Record used to send data to the client.
/// </summary>
[Serializable]
[DataContract]
internal record SearchBoxItem(string Id, string Name)
{
    [DataMember(Name = "id")]
    public string Id { get; set; } = Id;

    [DataMember(Name = "name")]
    public string Name { get; set; } = Name;
}
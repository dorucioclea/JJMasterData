﻿using Newtonsoft.Json;

namespace JJMasterData.Core.DataDictionary.Models.Actions;

public class ScriptAction : UserCreatedAction
{
    /// <summary>
    /// JS script that will be executed when the user clicks on the control.
    /// </summary>
    [JsonProperty("onClientClick")]
    // ReSharper disable once InconsistentNaming
    public string OnClientClick { get; set; }
    public override bool IsUserCreated => true;
}
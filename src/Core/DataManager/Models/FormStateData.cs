﻿#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JJMasterData.Commons.Extensions;
using JJMasterData.Core.DataDictionary.Models;

namespace JJMasterData.Core.DataManager.Models;

public class FormStateData
{
    public required IDictionary<string, object?>? UserValues { get; init; }

    public required IDictionary<string, object?> Values { get; init; }
    
    public required PageState PageState { get; init; }

    public FormStateData()
    {
        
    }
    
    [SetsRequiredMembers]
    public FormStateData(
        IDictionary<string, object?> values, 
        IDictionary<string, object?>? userValues,
        PageState pageState)
    {
        UserValues = userValues.DeepCopy();
        Values = values;
        PageState = pageState;
    }

    [SetsRequiredMembers]
    public FormStateData(
        IDictionary<string, object?> values,
        PageState pageState)
    {
        Values = values;
        PageState = pageState;
    }

}
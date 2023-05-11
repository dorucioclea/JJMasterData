﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace JJMasterData.Commons.Data.Entity;

/// <summary>
/// Specific relationship information between tables
/// </summary>
/// <remarks>2017-03-22 JJTeam</remarks>
[Serializable]
[DataContract]
public class ElementRelationship
{
    [DataMember(Name = "childElement")]
    public string ChildElement { get; set; }
        
    [DataMember(Name = "columns")]
    public List<ElementRelationshipColumn> Columns { get; set; }

    [DataMember(Name = "updateOnCascade")]
    public bool UpdateOnCascade { get; set; }

    [DataMember(Name = "deleteOnCascade")]
    public bool DeleteOnCascade { get; set; }

    [DataMember(Name = "viewType")]
    [Obsolete]
    public RelationshipViewType ViewType { get; set; }

    [DataMember(Name = "title")]
    [Obsolete]
    public string Title { get; set; }

    public ElementRelationship()
    {
        Columns = new List<ElementRelationshipColumn>();
    }

    public ElementRelationship(string childElement, params ElementRelationshipColumn[] columns)
    {
        Columns = new List<ElementRelationshipColumn>();
        ChildElement = childElement;
        ViewType = RelationshipViewType.List;
        if (columns != null)
            Columns.AddRange(columns);
    }

}
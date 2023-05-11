﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using JJMasterData.Commons.Data.Entity;

namespace JJMasterData.Core.DataDictionary;

[DataContract]
public class MetadataForm
{
    [DataMember(Name = "formfields")]
    public List<MetadataFormField> FormFields { get; set; }

    [DataMember(Name = "title")]
    public string Title { get; set; }

    [DataMember(Name = "subtitle")]
    public string SubTitle { get; set; }

    [DataMember(Name = "panels")]
    public List<FormElementPanel> Panels { get; set; }
    
    [DataMember(Name = "relationships")]
    public FormElementRelationshipList Relationships { get; set; }
    
    public MetadataForm()
    {
        Panels = new List<FormElementPanel>();
        FormFields = new List<MetadataFormField>();
        Relationships = new FormElementRelationshipList(new List<ElementRelationship>());
    }

    public MetadataForm(FormElement e) : this()
    {
        Title = e.Title;
        SubTitle = e.SubTitle;
        Panels = e.Panels;
        Relationships = e.Relationships;
        foreach (var f in e.Fields)
        {
            FormFields.Add(new MetadataFormField(f));
        }
    }
}
﻿using System.ComponentModel.DataAnnotations;
using JJMasterData.Core.UI.Components;

namespace JJMasterData.Core.DataDictionary.Models;

public sealed class Entity
{
    [Display(Name = "Name")]
    public required string Name { get; set; }
    
    [Display(Name = "Table Name")]
    public required string TableName { get; set; }
    
    [Display(Name = "Use Read Procedure")]
    public required bool UseReadProcedure { get; set; }
    
    [Display(Name = "Use Write Procedure")]
    public required bool UseWriteProcedure { get; set; }
    
    [Display(Name = "Read Procedure")]
    public required string ReadProcedureName { get; set; }
    
    [Display(Name = "Write Procedure")]
    public required string WriteProcedureName { get; set; }
    
    [Display(Name = "Title")]
    public required string Title { get; set; }
    
    [Display(Name = "Title Size")]
    public required HeadingSize TitleSize { get; set; }
    
    [Display(Name = "SubTitle")]
    public required string SubTitle { get; set; }
    
    [Display(Name = "Additional Info")]
    public required string Info { get; set; }

    public static Entity FromFormElement(FormElement formElement)
    {
        return new Entity
        {
            Name = formElement.Name,
            TableName = formElement.TableName,
            UseReadProcedure = formElement.UseReadProcedure,
            UseWriteProcedure = formElement.UseWriteProcedure,
            ReadProcedureName = formElement.ReadProcedureName,
            WriteProcedureName = formElement.WriteProcedureName,
            Title = formElement.Title,
            TitleSize = formElement.TitleSize,
            SubTitle = formElement.SubTitle,
            Info = formElement.Info
        };
    }
}
﻿using System.ComponentModel.DataAnnotations;

namespace JJMasterData.Core.DataDictionary.Models.Actions;


public class ViewAction : GridTableAction
{
    /// <summary>
    /// Nome padrão da ação
    /// </summary>
    public const string ActionName = "view";
    
    [Display(Name = "Show as Modal")]
    public bool ShowAsModal { get; set; }
    
    public ViewAction()
    {
        Name = ActionName;
        Tooltip = "View";
        ConfirmationMessage = "";
        IsDefaultOption = true;
        Icon = IconType.Eye;
        Order = 1;
    }



}
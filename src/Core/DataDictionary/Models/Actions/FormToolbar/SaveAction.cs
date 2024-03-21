#nullable enable

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace JJMasterData.Core.DataDictionary.Models.Actions;

/// <summary>
/// Action to save a DataPanel at a FormView.
/// </summary>
public sealed class SaveAction : FormToolbarAction, ISubmittableAction
{
    public const string ActionName = "save";

    public FormEnterKey EnterKeyBehavior { get; set; }
    
    [JsonProperty("isSubmit")]
    [Display(Name = "Is Submit")]
    public bool IsSubmit { get; set; }
    
    public SaveAction()
    {
        Order = 1;
        Name = ActionName;
        Icon = IconType.Check;
        Text = "Save";
        Location = FormToolbarActionLocation.Panel;
        Color = BootstrapColor.Primary;
        ShowAsButton = true;
        VisibleExpression = "exp: '{PageState}' <> 'View'";
    }
    public override BasicAction DeepCopy() => CopyAction();
}
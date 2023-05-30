#nullable enable


namespace JJMasterData.Core.DataDictionary.Actions.FormToolbar;

public class CancelAction : FormToolbarAction
{
    public const string ActionName = "cancel";

    public CancelAction()
    {
        Name = ActionName;
        Icon = IconType.Times;
        VisibleExpression = "exp:{pagestate} in ('INSERT','UPDATE')";
        Order = 0;
        ShowAsButton = true;
        Text = "Cancel";
    }
}
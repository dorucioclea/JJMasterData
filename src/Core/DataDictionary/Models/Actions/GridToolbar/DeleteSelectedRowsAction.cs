﻿namespace JJMasterData.Core.DataDictionary.Actions;

public class DeleteSelectedRowsAction : GridToolbarAction
{
    public const string ActionName = "deleteSelectedRows";

    public DeleteSelectedRowsAction()
    {
        Name = ActionName;
        Text = "Delete Selected";
        Icon = IconType.Trash;
        ShowAsButton = true;
        Order = 3;
        SetVisible(false);
    }
}
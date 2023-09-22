using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Actions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.Web.Html;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace JJMasterData.Core.Web.Components;

internal class GridToolbar
{
    private JJGridView GridView { get; set; }

    public GridToolbar(JJGridView gridView)
    {
        GridView = gridView;
    }

    public async Task<HtmlBuilder> GetHtmlBuilderAsync()
    {
        var toolbar = new JJToolbar();
        
        await foreach(var action in GetActionsHtmlBuilderEnumerable())
        {
            toolbar.Items.Add(action);
        }
        
        return toolbar.GetHtmlBuilder();
    }

    private async IAsyncEnumerable<HtmlBuilder> GetActionsHtmlBuilderEnumerable()
    {
        var actions = GridView.ToolBarActions.OrderBy(x => x.Order).ToList();
        var linkButtonFactory = GridView.ComponentFactory.Html.LinkButton;
        var formValues = await GridView.GetDefaultValuesAsync();
        var formStateData = new FormStateData(formValues,GridView.UserValues , PageState.List);
        
        foreach (var action in actions)
        {
            var linkButton = await linkButtonFactory.CreateGridToolbarButtonAsync(action, GridView,formStateData);
            if (!linkButton.Visible)
                continue;

            if (action is FilterAction { EnableScreenSearch: true })
            {
                yield return await GridView.Filter.GetHtmlToolBarSearch();
                continue;
            }
            
            switch (action)
            {
                case ExportAction when GridView.DataExportation.IsRunning():
                    linkButton.Spinner.Name = $"data-exportation-spinner-{GridView.DataExportation.Name}";
                    linkButton.Spinner.Visible = true;
                    break;
                case ImportAction when GridView.DataImportation.IsRunning():
                    linkButton.Spinner.Visible = true;
                    break;
                case FilterAction fAction:
                    if (fAction.ShowAsCollapse)
                        linkButton.Visible = false;
                    break;
            }

            yield return linkButton.GetHtmlBuilder();
        }
    }
    
}
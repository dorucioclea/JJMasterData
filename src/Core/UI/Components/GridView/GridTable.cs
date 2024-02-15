#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using JJMasterData.Core.UI.Html;

namespace JJMasterData.Core.UI.Components;

internal class GridTable(JJGridView gridView)
{
    internal GridSettings Settings { get; } = gridView.CurrentSettings;

    internal GridTableHeader Header { get; } = new(gridView);

    internal GridTableBody Body { get; } = new(gridView);

    public async Task<HtmlBuilder> GetHtmlBuilder()
    {
        var div = new HtmlBuilder(HtmlTag.Div);
        div.WithCssClass("pt-1");
        div.WithCssClassIf(Settings.IsResponsive,  "table-responsive");
        
        var table = new HtmlBuilder(HtmlTag.Table);
        table.WithCssClass("table");
        table.WithCssClassIf(Settings.IsCompact, "table-sm");
        table.WithCssClassIf(Settings.ShowBorder, "table-bordered");
        table.WithCssClassIf(Settings.ShowRowHover, "table-hover");
        table.WithCssClassIf(Settings.ShowRowStriped, "table-striped");
        table.WithCssClassIf(Settings.IsHeaderFixed, "table-fixed-header");

        table.Append(await Header.GetHtmlBuilderAsync());
        table.Append(await Body.GetHtmlBuilderAsync());

        div.Append(table);
        
        return div;
    }
}
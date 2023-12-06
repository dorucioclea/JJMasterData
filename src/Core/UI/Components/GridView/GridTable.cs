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
        var table = new HtmlBuilder(HtmlTag.Table);

        table.WithCssClass("table");

        table.WithCssClassIf(Settings.IsResponsive, "table-responsive");
        table.WithCssClassIf(Settings.ShowBorder, "table-bordered");
        table.WithCssClassIf(Settings.ShowRowHover, "table-hover");
        table.WithCssClassIf(Settings.ShowRowStriped, "table-striped");
        table.WithCssClassIf(Settings.IsHeaderFixed, "table-fixed-header");

        table.Append(await Header.GetHtmlBuilderAsync());
        table.Append(await Body.GetHtmlBuilderAsync());

        return table;
    }
}
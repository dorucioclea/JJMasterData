#nullable enable

using System;
using System.Collections.Generic;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.UI.Html;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.UI.Components;

internal sealed class GridPagination(JJGridView gridView)
{
    private readonly IStringLocalizer<MasterDataResources> _stringLocalizer  = gridView.StringLocalizer;
    private int _totalPages;
    private int _totalButtons;
    private int _startButtonIndex;
    private int _endButtonIndex;

    public HtmlBuilder GetHtmlBuilder()
    {
        _totalPages = (int)Math.Ceiling(gridView.TotalOfRecords / (double)gridView.CurrentSettings.RecordsPerPage);
        _totalButtons = gridView.CurrentSettings.TotalPaginationButtons;
        _startButtonIndex = (int)Math.Floor((gridView.CurrentPage - 1) / (double)_totalButtons) * _totalButtons + 1;
        _endButtonIndex = _startButtonIndex + _totalButtons;
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithCssClassIf(BootstrapHelper.Version > 3, "container-fluid p-0")
            .Append(HtmlTag.Div, div =>
            {
                div.WithCssClass("row justify-content-between");
                div.AppendDiv(div =>
                {
                    div.WithCssClass("col-sm-9");
                    div.AppendDiv(div =>
                    {
                        div.WithCssClass("d-flex");
                        div.AppendDiv(div =>
                        {
                            div.Append(GetPaginationHtmlBuilder());
                        });
                    });
                });
                div.Append(GetTotalRecordsHtmlBuilder());
            });

        return html;
    }

    private HtmlBuilder GetPaginationHtmlBuilder()
    {
        var ul = new HtmlBuilder(HtmlTag.Ul);
        ul.WithCssClass("pagination");

        if (_startButtonIndex > _totalButtons)
        {
            ul.Append(GetPageButton(1, IconType.AngleDoubleLeft, _stringLocalizer["First page"]));
            ul.Append(GetPageButton(_startButtonIndex - 1, IconType.AngleLeft, _stringLocalizer["Previous page"]));
        }

        for (int i = _startButtonIndex; i < _endButtonIndex; i++)
        {
            if (i > _totalPages || _totalPages <= 1)
                break;
            ul.Append(GetPageButton(i));
        }


        if (_endButtonIndex <= _totalPages)
        {
            ul.Append(GetPageButton(_endButtonIndex, IconType.AngleRight,
                _stringLocalizer["Next page"]));
            ul.Append(GetPageButton(_totalPages, IconType.AngleDoubleRight, _stringLocalizer["Last page"]));
        }

        var showJumpToPage = _endButtonIndex <= _totalPages || _startButtonIndex > _totalButtons;

        if (showJumpToPage && BootstrapHelper.Version >= 5)
        {
            ul.AppendRange(GetJumpToPageButtons());
        }

        return ul;
    }

    private IEnumerable<HtmlBuilder> GetJumpToPageButtons()
    {
        var jumpToPageName = gridView.Name + "-jump-to-page-input";
        var textBox = gridView.ComponentFactory.Controls.TextGroup.Create();

        textBox.Name = jumpToPageName;
        textBox.MinValue = 1;
        textBox.MaxValue = _totalPages;
        textBox.InputType = InputType.Number;

        textBox.Attributes["style"] = "display:none;width:150px";

        textBox.Attributes["onfocusout"] = gridView.Scripts.GetJumpToPageScript();
        textBox.PlaceHolder = _stringLocalizer["Jump to page..."];
        textBox.CssClass += " pagination-jump-to-page-input";


        yield return new HtmlBuilder(HtmlTag.Li)
            .WithCssClass("page-item")
            .Append(textBox.GetHtmlBuilder())
            .AppendDiv(div =>
            {
                div.WithId(jumpToPageName + "-invalid-feedback");
                div.WithCssClass("invalid-feedback");
                div.AppendText(_stringLocalizer["Page must be between 1 and {0}.", _totalPages]);
            });

        yield return new HtmlBuilder(HtmlTag.Li)
            .WithCssClass("page-item")
            .Append(new JJLinkButton(gridView.StringLocalizer)
            {
                ShowAsButton = false,
                Icon = IconType.SolidMagnifyingGlassArrowRight,
                CssClass = "btn pagination-jump-to-page-button",
                OnClientClick = $"GridViewHelper.showJumpToPage('{jumpToPageName}')"
            }.GetHtmlBuilder());
    }

    private HtmlBuilder GetPageButton(int page, IconType? icon = null, string? tooltip = null)
    {
        var li = new HtmlBuilder(HtmlTag.Li)
            .WithCssClass("page-item")
            .WithCssClassIf(page == gridView.CurrentPage, "active")
            .Append(HtmlTag.A, a =>
            {
                a.WithCssClass("page-link");
                a.WithStyle( "cursor:pointer; cursor:hand;");
                a.WithToolTip(tooltip);
                a.WithOnClick( $"javascript:{gridView.Scripts.GetPaginationScript(page)}");
                if (icon != null)
                {
                    a.AppendComponent(new JJIcon(icon.Value));
                }
                else
                {
                    a.AppendText(page.ToString());
                }
            });

        return li;
    }

    private HtmlBuilder GetTotalRecordsHtmlBuilder()
    {
        var div = new HtmlBuilder(HtmlTag.Div);
        div.WithCssClass($"col-sm-3 {BootstrapHelper.TextRight} text-muted");
        div.Append(HtmlTag.Label, label =>
        {
            label.WithAttribute("id", $"infotext_{gridView.Name}");
            label.WithCssClass("small");
            label.AppendText(_stringLocalizer["Showing"]);
            label.AppendText(" ");

            if (_totalPages <= 1)
            {
                label.Append(HtmlTag.Span, span =>
                {
                    span.WithAttribute("id", $"{gridView.Name}_totrows");
                    span.AppendText($" {gridView.TotalOfRecords:N0} ");
                    span.AppendText(_stringLocalizer["record(s)"]);
                });
            }
            else
            {
                var firstPageNumber = gridView.CurrentSettings.RecordsPerPage * gridView.CurrentPage -
                    gridView.CurrentSettings.RecordsPerPage + 1;

                var lastPageNumber =
                    gridView.CurrentSettings.RecordsPerPage * gridView.CurrentPage > gridView.TotalOfRecords
                        ? gridView.TotalOfRecords
                        : gridView.CurrentSettings.RecordsPerPage * gridView.CurrentPage;

                label.AppendText($"{firstPageNumber}-{lastPageNumber} {_stringLocalizer["From"]}");

                label.Append(HtmlTag.Span, span =>
                {
                    span.WithAttribute("id", $"{gridView.Name}_totrows");
                    span.AppendText(_stringLocalizer["{0} records", gridView.TotalOfRecords]);
                });
            }

            label.Append(HtmlTag.Br);

            if (_endButtonIndex <= _totalPages)
                label.AppendText(_stringLocalizer["{0} pages", _totalPages]);
        });

        div.AppendIf(gridView.EnableMultiSelect, HtmlTag.Br);
        div.AppendIf(gridView.EnableMultiSelect, GetEnableMultSelectTotalRecords);

        return div;
    }

    private HtmlBuilder GetEnableMultSelectTotalRecords()
    {
        var selectedValues = gridView.GetSelectedGridValues();
        string noRecordSelected = gridView.StringLocalizer["No record selected"];
        string oneRecordSelected = gridView.StringLocalizer["A selected record"];
        string multipleRecordsSelected = gridView.StringLocalizer["{0} selected records", selectedValues.Count];

        var span = new HtmlBuilder(HtmlTag.Span);
        span.WithCssClass("small");
        span.WithAttribute("id", $"selected-text-{gridView.Name}");
        span.WithAttribute("no-record-selected-label", noRecordSelected);
        span.WithAttribute("one-record-selected-label", oneRecordSelected);
        span.WithAttribute("multiple-records-selected-label", _stringLocalizer["{0} selected records"]);

        if (selectedValues.Count == 0)
        {
            span.AppendText(noRecordSelected);
        }
        else if (selectedValues.Count == 1)
        {
            span.AppendText(oneRecordSelected);
        }
        else
        {
            span.AppendText(multipleRecordsSelected);
        }

        return span;
    }
}
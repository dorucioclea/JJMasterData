﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.UI.Html;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.UI.Components;

internal class GridSortingConfig
{
    public string CurrentOrder { get; set; }

    public FormElement FormElement { get; set; }
    public JJComboBox ComboBox { get; set; }
    public string Name { get; set; }

    private readonly GridScripts _gridScripts;
    private readonly IComponentFactory _componentFactory;
    private readonly IStringLocalizer<MasterDataResources> _stringLocalizer;
    
    public GridSortingConfig(JJGridView grid)
    {
        if (grid == null)
            throw new ArgumentNullException(nameof(grid));

        _componentFactory = grid.ComponentFactory;
        _gridScripts = grid.Scripts;
        _stringLocalizer = grid.StringLocalizer;
        
        CurrentOrder = grid.CurrentOrder.ToQueryParameter();
        ComboBox = grid.ComponentFactory.Controls.ComboBox.Create();
        FormElement = grid.FormElement;
        Name = grid.Name;
    }

    public async Task<HtmlBuilder> GetHtmlBuilderAsync()
    {
        var dialog = _componentFactory.Html.ModalDialog.Create();
        dialog.Name = $"{Name}-sort-modal";
        dialog.Title = "Sort Fields";
        dialog.Size = ModalSize.Small;

        var btnSort = _componentFactory.Html.LinkButton.Create();
        btnSort.Name = $"btnsort_{Name}";
        btnSort.IconClass = IconType.Check.GetCssClass();
        btnSort.ShowAsButton = true;
        btnSort.Text = "Sort";
        btnSort.OnClientClick = _gridScripts.GetSortMultItemsScript();
        dialog.Buttons.Add(btnSort);

        var btnCancel = _componentFactory.Html.LinkButton.Create();
        btnCancel.Text = "Cancel";
        btnCancel.IconClass = IconType.Times.GetCssClass();
        btnCancel.ShowAsButton = true;
        btnCancel.OnClientClick = BootstrapHelper.GetCloseModalScript($"{Name}-sort-modal");

        dialog.Buttons.Add(btnCancel);

        var htmlContent = new HtmlBuilder(HtmlTag.Div)
            .Append(HtmlTag.Div, div =>
            {
                div.AppendComponent(new JJIcon("text-info fa fa-triangle-exclamation"));
                div.AppendText("&nbsp;");
                div.AppendText(_stringLocalizer["Drag and drop to change order."]);
            });
        await htmlContent.AppendAsync(HtmlTag.Table, async table =>
            {
                table.WithCssClass("table table-hover");
                table.Append(GetHtmlHeader());
                table.Append(await GetHtmlBody());
            });

        dialog.HtmlBuilderContent = htmlContent;

        return dialog.BuildHtml();
    }

    private HtmlBuilder GetHtmlHeader()
    {
        var thead = new HtmlBuilder(HtmlTag.Thead);
        thead.Append(HtmlTag.Tr, tr =>
        {
            tr.Append(HtmlTag.Th, th =>
            {
                th.WithAttribute("style", "width:50px");
                th.AppendText("#");
            });
            tr.Append(HtmlTag.Th, th =>
            {
                th.AppendText(_stringLocalizer["Column"]);
            });
            tr.Append(HtmlTag.Th, th =>
            {
                th.WithAttribute("style", "width:220px");
                th.AppendText(_stringLocalizer["Order"]);
            });
        });

        return thead;
    }

    private async Task<HtmlBuilder> GetHtmlBody()
    {
        var tbody = new HtmlBuilder(HtmlTag.Tbody);
        tbody.WithAttribute("id", $"sortable-{Name}");
        tbody.WithCssClass("ui-sortable jjsortable");
        
        ComboBox.DataItem.ShowIcon = true;
        ComboBox.DataItem.Items = new List<DataItemValue>
        {
            new("A", _stringLocalizer["Ascendant"], IconType.SortAmountAsc, null),
            new("D", _stringLocalizer["Descendant"], IconType.SortAmountDesc, null),
            new("N", _stringLocalizer["No Order"], IconType.Genderless, null)
        };

        var sortList = GetSortList();
        var fieldsList = sortList.Select(sort => FormElement.Fields[sort.FieldName]).ToList();

        foreach (var item in FormElement.Fields)
        {
            var f = fieldsList.Find(x => x.Name.Equals(item.Name));
            if (f == null)
                fieldsList.Add(item);
        }

        foreach (var item in fieldsList.Where(item => !item.VisibleExpression.Equals("val:0")))
        {
            ComboBox.Name = $"{item.Name}_order";
            ComboBox.SelectedValue = "N";

            var sort = sortList.Find(x => x.FieldName.Equals((string)item.Name));
            if (sort != null)
            {
                ComboBox.SelectedValue = sort.IsAsc ? "A" : "D";
            }

            await tbody.AppendAsync(HtmlTag.Tr, async tr =>
            {
                tr.WithAttribute((string)"id", (string)item.Name);
                tr.WithCssClass("ui-sortable-handle");
                tr.Append(HtmlTag.Td, td =>
                {
                    td.AppendComponent(new JJIcon("fa fa-arrows"));
                });
                tr.Append(HtmlTag.Td, td =>
                {
                    td.AppendText(_stringLocalizer[item.LabelOrName]);
                });
                await tr.AppendAsync(HtmlTag.Td, async td =>
                {
                    var comboBoxResult = (RenderedComponentResult)await ComboBox.GetResultAsync();
                    
                    td.Append((HtmlBuilder)comboBoxResult.HtmlBuilder);
                });
            });
        }

        return tbody;
    }

    private List<SortItem> GetSortList()
    {
        var listsort = new List<SortItem>();
        if (string.IsNullOrEmpty(CurrentOrder))
            return listsort;

        var orders = CurrentOrder.Split(',');
        foreach (string order in orders)
        {
            var parValue = order.Split(' ');
            var sort = new SortItem
            {
                FieldName = parValue[0].Trim(),
                IsAsc = true
            };
            if (parValue.Length > 1 && parValue[1].Trim().ToUpper().Equals("DESC"))
            {
                sort.IsAsc = false;
            }
            listsort.Add(sort);
        }

        return listsort;
    }

    private class SortItem
    {
        public string FieldName { get; set; }
        public bool IsAsc { get; set; }
    }

}
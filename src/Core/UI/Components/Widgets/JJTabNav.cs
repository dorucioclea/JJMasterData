﻿using System.Collections.Generic;
using JJMasterData.Commons.Configuration;
using JJMasterData.Commons.DI;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.Web.Html;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.Web.Components;

public class JJTabNav : JJComponentBase
{
    private int? _selectedTabIndex;
    public int SelectedTabIndex
    {
        get
        {
            _selectedTabIndex ??= RequestSelectedTabIndex();

            return (int)_selectedTabIndex;
        }
        set => _selectedTabIndex = value;
    }

    internal string InputHiddenSelectedTabName => $"selected_tab_{Name}";

    
    internal IHttpContext CurrentContext { get; }

    public List<NavContent> ListTab { get; set; }

    public JJTabNav(IHttpContext httpContext)
    {
        Name = "nav1";
        ListTab = new List<NavContent>();
        CurrentContext = httpContext;
    }

    internal override HtmlBuilder RenderHtml()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithAttributes(Attributes)
            .WithCssClass(CssClass)
            .Append(GetNavTabs())
            .Append(GetTabContent())
            .Append(HtmlTag.Input, i =>
            {
                i.WithAttribute("type", "hidden")
                 .WithNameAndId(InputHiddenSelectedTabName)
                 .WithAttribute("value", SelectedTabIndex.ToString());
            });

        return html;
    }

    private HtmlBuilder GetNavTabs()
    {
        var ul = new HtmlBuilder(HtmlTag.Ul)
            .WithAttribute("role", "tablist")
            .WithCssClass("nav nav-tabs");

        for (int i = 0; i < ListTab.Count; i++)
        {
            NavContent nav = ListTab[i];
            string navId = $"{Name}_nav_{i}";

            var index = i;
            ul.Append(HtmlTag.Li, li =>
            {
                li.WithCssClassIf(BootstrapHelper.Version > 3, "nav-item")
                  .WithCssClassIf(SelectedTabIndex == index && BootstrapHelper.Version == 3, "active")
                  .WithAttribute("role", "presentation")
                  .Append(HtmlTag.A, a =>
                  {
                      a.WithAttribute("href", $"#{navId}")
                       .WithAttribute("aria-controls", navId)
                       .WithAttribute("jj-tabindex", index.ToString())
                       .WithAttribute("jj-objectid", InputHiddenSelectedTabName)
                       .WithAttribute("aria-selected", SelectedTabIndex == index ? "true" : "false")
                       .WithAttribute("role", "tab")
                       .WithDataAttribute("toggle", "tab")
                       .WithCssClass("jj-tab-link nav-link")
                       .WithCssClassIf(SelectedTabIndex == index && BootstrapHelper.Version > 3, "active")
                       .AppendText(nav.Title);
                  });
            });
        }

        return ul;
    }

    private HtmlBuilder GetTabContent()
    {
        var tabContent = new HtmlBuilder(HtmlTag.Div)
            .WithCssClass("tab-content");

        for (int i = 0; i < ListTab.Count; i++)
        {
            NavContent nav = ListTab[i];
            var divContent = new HtmlBuilder(HtmlTag.Div)
                .WithAttribute("id", $"{Name}_nav_{i}")
                .WithAttribute("role", "tabpanel")
                .WithCssClass("tab-pane fade")
                .WithCssClassIf(SelectedTabIndex == i, "active" + BootstrapHelper.Show)
                .Append(nav.HtmlContent);

            tabContent.Append(divContent);
        }

        return tabContent;
    }

    private int RequestSelectedTabIndex()
    {
        string tabIndex = CurrentContext.Request[InputHiddenSelectedTabName];
        return int.TryParse(tabIndex, out var nIndex) ? nIndex : 0;
    }

}

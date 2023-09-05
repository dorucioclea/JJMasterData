﻿using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.Web.Html;
using System;
using JJMasterData.Core.UI.Components;

namespace JJMasterData.Core.Web.Components;

/// <summary>
/// This is a simple content container
/// </summary>
public class JJCard : HtmlComponent
{
    public string Title { get; set; }

    public string SubTitle { get; set; }

    public PanelLayout Layout { get; set; }

    public PanelColor Color { get; set; } = PanelColor.Default;

    public HtmlBuilder HtmlBuilderContent { get; set; }

    private bool HasTitle => !string.IsNullOrEmpty(Title) | !string.IsNullOrEmpty(SubTitle);

    internal override HtmlBuilder BuildHtml()
    {
        HtmlBuilder html;
        if (Layout == PanelLayout.Well)
            html = GetHtmlWell();
        else if (Layout == PanelLayout.NoDecoration)
            html = GetHtmlNoDecoration();
        else
            html = GetHtmlPanel();

        if (BootstrapHelper.Version > 3)
        {
            return new HtmlBuilder(HtmlTag.Div)
                .WithCssClass("mb-3")
                .Append(html);
        }

        return html;
    }



    private HtmlBuilder GetHtmlPanel()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithAttributes(Attributes)
            .WithNameAndId(Name)
            .WithCssClass(CssClass)
            .WithCssClass(BootstrapHelper.GetPanel(Color.ToString().ToLower()));

        html.AppendIf(!string.IsNullOrEmpty(Title), HtmlTag.Div, header =>
        {
            header.WithCssClass(BootstrapHelper.GetPanelHeading(Color.ToString().ToLower()));
            header.AppendText(Title);
        });

        html.Append(HtmlTag.Div, d =>
        {
            d.WithCssClass(BootstrapHelper.PanelBody);
            if (!string.IsNullOrEmpty(SubTitle))
            {
                var title = new JJTitle(null, SubTitle);
                d.Append(title.GetHtmlBlockquote());
            }
            d.Append(HtmlBuilderContent);
        });

        return html;
    }

    private HtmlBuilder GetHtmlNoDecoration()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithAttributes(Attributes)
            .WithNameAndId(Name)
            .WithCssClass(CssClass);

        if (HasTitle)
        {
            var title = new JJTitle(Title, SubTitle);
            html.Append(title.GetHtmlBlockquote());
        }

        html.Append(HtmlBuilderContent);

        return html;
    }

    private HtmlBuilder GetHtmlWell()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithAttributes(Attributes)
            .WithNameAndId(Name)
            .WithCssClass(CssClass);


        if (BootstrapHelper.Version == 3)
            html.WithCssClass("well");
        else
            html.WithCssClass("card card-body bg-light");

        if (HasTitle)
        {
            var title = new JJTitle(Title, SubTitle);
            html.Append(title.GetHtmlBlockquote());
        }

        html.Append(HtmlBuilderContent);

        return html;
    }

}
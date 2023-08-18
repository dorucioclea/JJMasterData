﻿using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Html;

namespace JJMasterData.Core.Web.Components;

public class JJSpinner : HtmlComponent
{
    public JJSpinner()
    {
        CssClass += "spinner-grow spinner-grow-sm text-info";

        if (BootstrapHelper.Version == 3)
            CssClass += " jj-blink";
    }

    internal override HtmlBuilder BuildHtml()
    {
        var html = new HtmlBuilder(HtmlTag.Span)
            .WithNameAndId(Name)
            .WithCssClass(CssClass)
            .WithAttributes(Attributes)
            .WithAttribute("role", "status")
            .AppendIf(BootstrapHelper.Version == 3,()=> new JJIcon(IconType.Circle).BuildHtml())
            .AppendIf(BootstrapHelper.Version != 3, HtmlTag.Span, s =>
            {
                s.WithCssClass("visually-hidden");
                s.AppendText("Loading...");

            });

        return html;
    }
}
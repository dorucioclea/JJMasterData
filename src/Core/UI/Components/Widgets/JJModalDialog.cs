﻿using System.Collections.Generic;
using JJMasterData.Commons.Extensions;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Html;

namespace JJMasterData.Core.Web.Components;
public class JJModalDialog : HtmlComponent
{
    public string Title { get; set; }

    public string HtmlContent { get; set; }
    
    public HtmlBuilder HtmlBuilderContent { get; set; }
    
    public List<JJLinkButton> Buttons { get; set; }

    public ModalSize Size { get; init; }

    public JJModalDialog()
    {
        Name = "jjmodal";
        Size = ModalSize.Small;
        Buttons = new List<JJLinkButton>();
    }

    internal override HtmlBuilder BuildHtml()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithAttributes(Attributes)
            .WithAttribute("id", Name)
            .WithCssClass("modal")
            .WithCssClass(CssClass)
            .WithAttribute("role", "dialog")
            .WithAttribute("aria-hidden", "true")
            .WithAttribute("aria-labelledby", $"{Name}-label")
            .Append(HtmlTag.Div, div =>
            {
                div.WithCssClass($"modal-dialog {Size.GetDescription()}");
                div.Append(HtmlTag.Div, content =>
                {
                    content.WithCssClass("modal-content");
                    content.Append(GetHtmlHeader());
                    
                    content.Append(HtmlTag.Div, body =>
                    {
                        body.WithCssClass("modal-body")
                            .AppendTextIf(!string.IsNullOrEmpty(HtmlContent), HtmlContent!)
                            .Append(HtmlBuilderContent);
                    });
                    content.AppendIf(Buttons.Count > 0, HtmlTag.Div, footer =>
                    {
                        footer.WithCssClass("modal-footer");
                        foreach (var btn in Buttons)
                        {
                            footer.AppendComponent(btn);
                        }
                    });

                });
            });

        return html;
    }

    private HtmlBuilder GetHtmlHeader()
    {
        var btn = JJAlert.GetCloseButton("modal");
        var header = new HtmlBuilder(HtmlTag.Div)
            .WithCssClass("modal-header")
            .AppendIf(BootstrapHelper.Version == 3, ()=>btn)
            .Append(HtmlTag.H4, h4 =>
            {
                h4.WithCssClass("modal-title")
                  .AppendText(Title);
            })
            .AppendIf(BootstrapHelper.Version > 3,()=> btn);

        return header;
    }
}

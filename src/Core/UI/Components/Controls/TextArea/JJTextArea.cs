﻿using System.Threading.Tasks;
using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.Http.Abstractions;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.UI.Components.Controls;
using JJMasterData.Core.Web.Html;
using JJMasterData.Core.Web.Http.Abstractions;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.Web.Components;

public class JJTextArea : ControlBase
{
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }
    public int Rows { get; set; }

    public JJTextArea(IFormValues formValues,IStringLocalizer<JJMasterDataResources> stringLocalizer) : base(formValues)
    {
        StringLocalizer = stringLocalizer;
        Attributes.Add("class", "form-control");
        Rows = 5;
    }

    protected override async Task<ComponentResult> BuildResultAsync()
    {
        var html = new HtmlBuilder(HtmlTag.TextArea)
            .WithAttributes(Attributes)
            .WithNameAndId(Name)
            .WithCssClass(CssClass)
            .WithToolTip(Tooltip)
            .WithAttributeIf(!string.IsNullOrWhiteSpace(PlaceHolder), "placeholder", PlaceHolder)
            .WithAttribute("cols", "20")
            .WithAttribute("rows", Rows)
            .WithAttribute("maximum-limit-of-characters-label", StringLocalizer["Maximum limit of {0} characters!"])
            .WithAttribute("characters-remaining-label", StringLocalizer["({0} characters remaining)"])
            .WithAttributeIf(MaxLength > 0, "maxlength", MaxLength.ToString())
            .WithAttributeIf(ReadOnly, "readonly", "readonly")
            .WithAttributeIf(!Enabled, "disabled", "disabled")
            .AppendText(Text);

        var result = new RenderedComponentResult(html);
        
        return await Task.FromResult(result);
    }
}
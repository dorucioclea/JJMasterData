﻿#nullable enable
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.Http.Abstractions;
using JJMasterData.Core.UI.Html;
using Microsoft.Extensions.Localization;


namespace JJMasterData.Core.UI.Components;

public class JJIconPicker(
    IStringLocalizer<MasterDataResources> stringLocalizer,
    IMasterDataUrlHelper urlHelper,
    IControlFactory<JJComboBox> comboBoxFactory,
    IFormValues formValues) : ControlBase(formValues)
{
    
    public IconType? SelectedIcon { get; set; }
    
    protected override async Task<ComponentResult> BuildResultAsync()
    {
        var comboBox = comboBoxFactory.Create();
        comboBox.Name = Name;
        if(SelectedIcon is not null)
        {
            comboBox.SelectedValue = ((int)SelectedIcon).ToString();
        }
        comboBox.DataItem = new FormElementDataItem
        {
            DataItemType = DataItemType.Manual,
            Items = IconHelper.GetIconList().Select(icon => new DataItemValue
            {
                Id = ((int)icon).ToString(),
                Description = icon.ToString(),
                Icon = icon
            }).ToList(),
            FirstOption = FirstOptionMode.Choose,
            ShowIcon = true,
        };
        
        comboBox.Attributes["data-live-search"] = "true";
        comboBox.Attributes["data-virtual-scroll"] = "true";
        comboBox.Attributes["data-size"] = "false";
        comboBox.Attributes["data-sanitize"] = "false";
        comboBox.Attributes["data-none-results-text"] = stringLocalizer["No icons found."];
        var div = new HtmlBuilder(HtmlTag.Div);
        div.WithCssClass("input-group");
        await div.AppendControlAsync(comboBox);
        div.AppendDiv(div =>
        {
            var tooltip = stringLocalizer["Search Icon"];
            div.WithCssClass("btn btn-default");
            div.WithToolTip(tooltip);
            div.AppendComponent(new JJIcon(IconType.Search));
            var url = urlHelper.Action("Index", "Icons", new { inputId = Name, Area="MasterData" });
            div.WithAttribute("onclick", $"iconsModal.showUrl('{url}', '{tooltip}', '{(int)ModalSize.ExtraLarge}')");
        });

        return new RenderedComponentResult(div);
    }
}
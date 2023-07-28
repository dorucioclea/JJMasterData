using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Factories;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace JJMasterData.Web.TagHelpers;

public class JJDataPanelTagHelper : TagHelper
{
    private IFormElementComponentFactory<JJDataPanel> DataPanelFactory { get; }

    [HtmlAttributeName("name")] 
    public string? Name { get; set; }
    
    [HtmlAttributeName("element-name")] 
    public string? ElementName { get; set; }
    
    [HtmlAttributeName("configure")] 
    public Action<JJDataPanel>? Configure { get; set; }

    public JJDataPanelTagHelper(IFormElementComponentFactory<JJDataPanel> dataPanelFactory)
    {
        DataPanelFactory = dataPanelFactory;
    }
    
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var form = await DataPanelFactory.CreateAsync(ElementName ?? throw new ArgumentNullException(nameof(ElementName)));

        if (!string.IsNullOrEmpty(Name))
        {
            form.Name = Name;
        }

        Configure?.Invoke(form);
        
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Content.SetHtmlContent(await form.GetHtmlAsync());
    }
}
using System.Globalization;
using System.Threading.Tasks;
using JJMasterData.Commons.Cryptography;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.UI.Components.Controls;
using JJMasterData.Core.Web.Html;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.Web.Components;

public class JJSlider : ControlBase
{
    private IComponentFactory<JJTextBox> TextBoxFactory { get; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double? Value { get; set; }
    public double Step { get; set; } = 1;
    public bool ShowInput { get; set; } = true;
    public int NumberOfDecimalPlaces { get; set; }
    
    public JJSlider(IHttpRequest httpRequest, IComponentFactory<JJTextBox> textBoxFactory) : base(httpRequest)
    {
        TextBoxFactory = textBoxFactory;
    }

    protected override async Task<ComponentResult> BuildResultAsync()
    {
        var html = new HtmlBuilder(HtmlTag.Div)
            .WithCssClass("row")
            .WithCssClass(CssClass)
            .Append(HtmlTag.Div, col =>
            {
                col.WithCssClass(ShowInput ? "col-sm-9" : "col-sm-12");
                col.WithCssClassIf(BootstrapHelper.Version > 3, "d-flex justify-content-end align-items-center");
                col.Append(GetHtmlSlider());
            });

        if (ShowInput)
        {
            var number = TextBoxFactory.Create();
            number.InputType = InputType.Number;
            number.Name = $"{Name}-value";
            number.MinValue = MinValue;
            number.Enabled = Enabled;
            number.Text = Value.ToString();
            number.NumberOfDecimalPlaces = NumberOfDecimalPlaces;
            number.MaxValue = MaxValue;
            number.CssClass = "jjslider-value";
            number.Attributes["step"] = Step.ToString();

            await html.AppendAsync(HtmlTag.Div, async row =>
            {
                row.WithCssClass("col-sm-3");
                row.Append(await number.GetHtmlBuilderAsync());
            });
        }
        
        var result = new RenderedComponentResult(html);
        
        return await Task.FromResult(result);
    }
    
    private HtmlBuilder GetHtmlSlider()
    {
        var slider = new HtmlBuilder(HtmlTag.Input)
           .WithAttributes(Attributes)
           .WithAttribute("type", "range")
           .WithNameAndId(Name)
           .WithCssClass("jjslider form-range")
           .WithAttributeIf(!Enabled,"disabled")
           .WithAttributeIf(NumberOfDecimalPlaces > 0 , "jjdecimalplaces", NumberOfDecimalPlaces.ToString())
           .WithAttribute("min", MinValue.ToString(CultureInfo.InvariantCulture))
           .WithAttribute("max", MaxValue.ToString(CultureInfo.InvariantCulture))
           .WithAttribute("step", Step.ToString(CultureInfo.InvariantCulture))
           .WithAttributeIf(Value.HasValue, "value", Value?.ToString());

        return slider;
    }

}
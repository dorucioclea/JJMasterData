using JJMasterData.Commons.Cryptography;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.Web.Factories;

internal class SliderFactory : IControlFactory<JJSlider>
{
    private IHttpRequest HttpRequest { get; }
    private IComponentFactory<JJTextBox> TextBoxFactory { get; }


    public SliderFactory(IHttpRequest httpRequest, IComponentFactory<JJTextBox> textBoxFactory)
    {
        HttpRequest = httpRequest;
        TextBoxFactory = textBoxFactory;
    }
    
    public JJSlider Create()
    {
        return new JJSlider(HttpRequest,TextBoxFactory);
    }

    public JJSlider Create(FormElement formElement, FormElementField field, ControlContext context)
    {
        var slider = Create();
        slider.Name = field.Name;
        slider.NumberOfDecimalPlaces = field.NumberOfDecimalPlaces;
        slider.MinValue = (double)(field.Attributes[FormElementField.MinValueAttribute] ?? 0f);
        slider.MaxValue = (double)(field.Attributes[FormElementField.MaxValueAttribute] ?? 100);
        slider.Step = (double)field.Attributes![FormElementField.StepAttribute];
        slider.Value = !string.IsNullOrEmpty(context.Value?.ToString()) ? double.Parse(context.Value.ToString()!) : null;
        return slider;
    }
}
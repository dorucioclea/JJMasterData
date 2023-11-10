#nullable enable

using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.UI.Components;

namespace JJMasterData.Core.UI.Routing;

public class RouteContext
{
    public string? ElementName { get; set; }
    public string? ParentElementName { get; set; }
    public ComponentContext ComponentContext { get; set; }
    
    public RouteContext()
    {
        ComponentContext = ComponentContext.RenderComponent;
    }

    internal RouteContext(string? elementName, string? parentElementName, ComponentContext componentContext)
    {
        ComponentContext = componentContext;
        ElementName = elementName;
        ParentElementName = parentElementName;
    }

    public static RouteContext FromFormElement(FormElement formElement,ComponentContext context)
    {
        return new RouteContext(formElement.Name, formElement.ParentName, context);
    }
    
    public bool CanRender(string elementName)
    {
        if (ElementName is null)
            return true;
        
        if (ParentElementName is not null)
            return ParentElementName == elementName || ParentElementName == elementName;

        return IsCurrentFormElement(elementName);
    }
    
    public bool IsCurrentFormElement(string elementName)
    {
        return ElementName is null || elementName.Equals(ElementName);
    }
}

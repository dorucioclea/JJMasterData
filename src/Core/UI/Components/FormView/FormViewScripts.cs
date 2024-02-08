#nullable enable
using System.Collections.Generic;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.UI.Routing;

namespace JJMasterData.Core.UI.Components;

public class FormViewScripts(JJFormView formView)
{
    private string GetEncryptedRouteContext(ComponentContext context)
    {
        var routeContext = RouteContext.FromFormElement(formView.FormElement, context);
        return formView.EncryptionService.EncryptRouteContext(routeContext);
    }

    public string GetShowInsertSuccessScript()
    {
        var encryptedRouteContext = GetEncryptedRouteContext(ComponentContext.GridViewReload);
        return $"FormViewHelper.showInsertSuccess('{formView.Name}', '{encryptedRouteContext}')";
    }

    public string GetInsertSelectionScript(Dictionary<string, object?> values)
    {
        var encryptedRouteContext = GetEncryptedRouteContext(ComponentContext.InsertSelection);
        var encryptedValues = formView.EncryptionService.EncryptDictionary(values);
        return $"FormViewHelper.insertSelection('{formView.Name}', '{encryptedValues}', '{encryptedRouteContext}')";
    }

    public string GetSetPageStateScript(PageState pageState)
    {
        var encryptedRouteContext = GetEncryptedRouteContext(ComponentContext.FormViewReload);
        return $"FormViewHelper.setPageState('{formView.Name}','{(int)pageState}', '{encryptedRouteContext}')";
    }
    
    public string GetSetPanelStateScript(PageState pageState)
    {
        var encryptedRouteContext = GetEncryptedRouteContext(ComponentContext.FormViewReload);
        return $"FormViewHelper.setPanelState('{formView.Name}','{(int)pageState}', '{encryptedRouteContext}')";
    }

}
using JJMasterData.Commons.Security.Cryptography.Abstractions;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.UI.Routing;

namespace JJMasterData.Core.UI.Components;

internal class DataPanelScripts
{

    private readonly DataPanelControl _dataPanelControl;
    private IEncryptionService EncryptionService => _dataPanelControl.EncryptionService;
    

    public DataPanelScripts(DataPanelControl dataPanelControl)
    {
        _dataPanelControl = dataPanelControl;
    }

    
    public string GetReloadPanelScript(string fieldName)
    {
        var componentName = _dataPanelControl.Name;
        
        var routeContext = EncryptionService.EncryptRouteContext(RouteContext.FromFormElement(_dataPanelControl.FormElement,ComponentContext.DataPanelReload));
        
        return $"DataPanelHelper.reload('{componentName}','{fieldName}','{routeContext}');";
    }
}
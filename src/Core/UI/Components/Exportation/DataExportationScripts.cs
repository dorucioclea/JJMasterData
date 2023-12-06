﻿using JJMasterData.Commons.Security.Cryptography.Abstractions;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.UI.Routing;

namespace JJMasterData.Core.UI.Components;

internal class DataExportationScripts(string componentName,FormElement formElement, IEncryptionService encryptionService)
{
    private string Name { get; } = componentName;
    private FormElement FormElement { get; } = formElement;
    private IEncryptionService EncryptionService { get; } = encryptionService;

    public DataExportationScripts(JJDataExportation dataExportation) : this(dataExportation.Name, dataExportation.FormElement, dataExportation.EncryptionService)
    {
    }


    private string EncryptedRouteContext
    {
        get
        {
            var routeContext = RouteContext.FromFormElement(FormElement, ComponentContext.DataExportation);
            var encryptedRouteContext = EncryptionService.EncryptRouteContext(routeContext);
            return encryptedRouteContext;
        }
    }

    public string GetStartExportationScript()
    {
        return $"DataExportationHelper.startExportation( '{Name}','{EncryptedRouteContext}');";
    }
    
    public string GetStopExportationScript(string stopMessage)
    {
        return $"DataExportationHelper.stopExportation('{Name}','{EncryptedRouteContext}','{stopMessage}');";
    }
    
    public string GetExportPopupScript()
    {
        return $"DataExportationHelper.openExportPopup('{Name}','{EncryptedRouteContext}');";
    }
}
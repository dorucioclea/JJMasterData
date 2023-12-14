using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.Http;
using JJMasterData.Core.Http.Abstractions;
using Microsoft.Extensions.Logging;

namespace JJMasterData.Core.UI.Components;

internal class LookupFactory(IFormValues formValues,
        LookupService lookupService,
        MasterDataUrlHelper urlHelper,
        IComponentFactory componentFactory,
        ILoggerFactory loggerFactory)
    : IControlFactory<JJLookup>
{
    private IFormValues FormValues { get; } = formValues;
    private LookupService LookupService { get; } = lookupService;
    private MasterDataUrlHelper UrlHelper { get; } = urlHelper;
    private IComponentFactory ComponentFactory { get; } = componentFactory;

    private ILoggerFactory LoggerFactory { get; } = loggerFactory;

    public JJLookup Create()
    {
        return new JJLookup(
            null,
            null,
            FormValues,
            LookupService,
            ComponentFactory);
    }

    public JJLookup Create(FormElement formElement, FormElementField field, ControlContext controlContext)
    {
        var lookup = new JJLookup(
            field,
            controlContext,
            FormValues,
            LookupService,
            ComponentFactory);
        
        return lookup;
    }

    
}
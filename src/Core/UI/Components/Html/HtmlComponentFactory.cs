# nullable enable

using System;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.Http;
using JJMasterData.Core.Http.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.UI.Components;

public class HtmlComponentFactory(IStringLocalizer<MasterDataResources> stringLocalizer,
    IHttpContext currentContext,
    MasterDataUrlHelper urlHelper,
    IServiceProvider serviceProvider)
{
    private readonly MasterDataUrlHelper _urlHelper = urlHelper;

    public AlertFactory Alert => new();

    public CardFactory Card => new();
    
    public CollapsePanelFactory CollapsePanel => new(currentContext.Request.Form);
    
    public IconFactory Icon => new();
    
    public ImageFactory Image =>  new(currentContext);
    
    public LabelFactory Label => new(stringLocalizer);
    
    public LinkButtonFactory LinkButton => serviceProvider.GetRequiredService<LinkButtonFactory>();
    
    public MessageBoxFactory MessageBox =>  new(stringLocalizer);
    
    public ModalDialogFactory ModalDialog => new();
    
    public SpinnerFactory Spinner => new();
    
    public NavFactory TabNav => new(currentContext.Request.Form);
    
    public TitleFactory Title => new();
    
    public ToolbarFactory Toolbar => new();
    
    public ValidationSummaryFactory ValidationSummary =>  new(stringLocalizer);
    
}

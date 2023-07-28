using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Http.Abstractions;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.Web.Factories;

internal class UploadAreaFactory : IComponentFactory<JJUploadArea>
{
    private IHttpContext HttpContext { get; }
    private IUploadAreaService UploadAreaService { get; }
    private JJMasterDataUrlHelper UrlHelper { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }

    public UploadAreaFactory(
        IHttpContext httpContext,
        IUploadAreaService uploadAreaService, 
        JJMasterDataUrlHelper urlHelper,
        IStringLocalizer<JJMasterDataResources> stringLocalizer)
    {
        HttpContext = httpContext;
        UploadAreaService = uploadAreaService;
        UrlHelper = urlHelper;
        StringLocalizer = stringLocalizer;
    }

    public JJUploadArea Create()
    {
        return new JJUploadArea(HttpContext,UploadAreaService,UrlHelper, StringLocalizer);
    }
}   
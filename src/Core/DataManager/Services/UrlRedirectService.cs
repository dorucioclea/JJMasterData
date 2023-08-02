using System.Threading.Tasks;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Actions.UserCreated;
using JJMasterData.Core.DataManager.Models;
using JJMasterData.Core.DataManager.Services.Abstractions;

namespace JJMasterData.Core.DataManager.Services;

public class UrlRedirectService : IUrlRedirectService
{
    private IFormValuesService FormValuesService { get; }
    private IExpressionsService ExpressionsService { get; }

    public UrlRedirectService(IFormValuesService formValuesService, IExpressionsService expressionsService)
    {
        FormValuesService = formValuesService;
        ExpressionsService = expressionsService;
    }
    
    public async Task<UrlRedirectModel> GetUrlRedirectAsync(FormElement formElement,ActionMap actionMap, PageState pageState)
    {
        var urlAction = (UrlRedirectAction)formElement.Fields[actionMap?.FieldName].Actions.Get(actionMap!.ActionName);

        var values = await FormValuesService.GetFormValuesAsync(formElement,pageState);
        
        var parsedUrl = ExpressionsService.ParseExpression(urlAction.UrlRedirect, pageState, false, values);
        
        var model = new UrlRedirectModel(urlAction.UrlAsPopUp, urlAction.PopUpTitle, parsedUrl);
        return model;
    }
}
using JJMasterData.Core.Http.Abstractions;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.Web.Components;

public class NavFactory : IComponentFactory<JJTabNav>
{
    private readonly IFormValues _formValues;

    public NavFactory(IFormValues formValues)
    {
        _formValues = formValues;
    }
    
    public JJTabNav Create()
    {
        return new JJTabNav(_formValues);
    }
}
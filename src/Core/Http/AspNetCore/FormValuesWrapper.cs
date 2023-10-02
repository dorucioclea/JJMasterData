#if NET

#nullable enable

using JJMasterData.Core.Http.Abstractions;
using Microsoft.AspNetCore.Http;

namespace JJMasterData.Core.Http.AspNetCore;

internal class FormValuesWrapper : IFormValues
{
    private IFormCollection? FormCollection { get; set; }

    public void Invalidate()
    {
        FormCollection = null;
    }

    public bool ContainsFormValues() => FormCollection is not null;

    public string? this[string key] => FormCollection?[key];

    public FormValuesWrapper(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext.Request.HasFormContentType)
        {
            FormCollection = httpContext.Request.Form;
        }
    }
    public IFormFile? GetFile(string file) => FormCollection?.Files[file];
}
#endif
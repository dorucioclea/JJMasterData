#nullable enable
using System.Threading.Tasks;

#if NET
using Microsoft.AspNetCore.Mvc;
#endif

namespace JJMasterData.Core.UI.Components;

public class RedirectComponentResult(string url) : ComponentResult
#if NET
,IActionResult
#endif  
{
    public override string Content { get; } = url;

#if NET 
    public async Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
    {
        await new RedirectResult(Content).ExecuteResultAsync(context);
    }
#endif
}
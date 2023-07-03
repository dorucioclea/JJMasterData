using JJMasterData.Core.Web.Components.Scripts;
using Microsoft.Extensions.DependencyInjection;

namespace JJMasterData.Core.Configuration;

public static class ScriptHelperServiceExtensions
{
    public static void AddScriptHelpers(this IServiceCollection services)
    {
        services.AddTransient<DataExpScriptHelper>();
        services.AddTransient<FormViewScriptHelper>();
        services.AddTransient<GridViewScriptHelper>();
        services.AddTransient<GridViewToolbarScriptHelper>();
    }
}
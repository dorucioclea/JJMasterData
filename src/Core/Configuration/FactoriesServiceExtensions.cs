using System;
using System.Linq;
using JJMasterData.Commons.Configuration;
using JJMasterData.Core.UI.Components.GridView;
using JJMasterData.Core.Web.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace JJMasterData.Core.Configuration;

public static class FactoriesServiceExtensions
{
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddTransient<DataExportationFactory>().AllowLazyInicialization();
        services.AddTransient<DataImportationFactory>().AllowLazyInicialization();
        
        services.AddTransient<ComboBoxFactory>();
        services.AddTransient<CheckBoxFactory>();
        services.AddTransient<ControlsFactory>();

        services.AddTransient<FormUploadFactory>();
        services.AddTransient<FileDownloaderFactory>();
        
        services.AddTransient<AuditLogViewFactory>().AllowLazyInicialization();
        services.AddTransient<DataPanelFactory>().AllowLazyInicialization();
        services.AddTransient<FormViewFactory>().AllowLazyInicialization();
        services.AddTransient<GridViewFactory>().AllowLazyInicialization();
        
        services.AddTransient<ComponentsFactory>();
        services.AddTransient<LookupFactory>();
        services.AddTransient<SearchBoxFactory>();
        services.AddTransient<TextAreaFactory>();
        services.AddTransient<SliderFactory>();
        services.AddTransient<TextBoxFactory>();
        services.AddTransient<TextRangeFactory>();
        services.AddTransient<UploadAreaFactory>();
        services.AddTransient<TextFileFactory>();

        return services;
    }

}
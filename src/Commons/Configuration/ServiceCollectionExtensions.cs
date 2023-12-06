using System;
using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Commons.Data;
using JJMasterData.Commons.Data.Entity.Providers;
using JJMasterData.Commons.Data.Entity.Repository;
using JJMasterData.Commons.Data.Entity.Repository.Abstractions;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Logging;
using JJMasterData.Commons.Logging.Db;
using JJMasterData.Commons.Logging.File;
using JJMasterData.Commons.Security.Cryptography;
using JJMasterData.Commons.Security.Cryptography.Abstractions;
using JJMasterData.Commons.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace JJMasterData.Commons.Configuration;

public static class ServiceCollectionExtensions
{
    public static MasterDataServiceBuilder AddJJMasterDataCommons(this IServiceCollection services)
    {
        var builder = new MasterDataServiceBuilder(services);

        services.AddMasterDataCommonsServices();
        
        return builder;
    }

    public static MasterDataServiceBuilder AddJJMasterDataCommons(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = new MasterDataServiceBuilder(services);

        builder.Services.Configure<MasterDataCommonsOptions>(configuration.GetJJMasterData());
        
        services.AddMasterDataCommonsServices(configuration);

        return builder;
    }

    public static MasterDataServiceBuilder AddJJMasterDataCommons(this IServiceCollection services, Action<MasterDataCommonsOptions> configure, IConfiguration loggingConfiguration = null)
    {
        var builder = new MasterDataServiceBuilder(services);

        services.AddMasterDataCommonsServices(loggingConfiguration);
        services.PostConfigure(configure);

        return builder;
    }
    
    private static IServiceCollection AddMasterDataCommonsServices(this IServiceCollection services,IConfiguration configuration = null)
    {
        services.AddOptions<MasterDataCommonsOptions>().BindConfiguration("JJMasterData");

        services.AddLocalization();
        services.AddMemoryCache();
        services.AddSingleton<ResourceManagerStringLocalizerFactory>();
        services.AddSingleton<IStringLocalizerFactory, MasterDataStringLocalizerFactory>();
        services.Add(new ServiceDescriptor(typeof(IStringLocalizer<>), typeof(MasterDataStringLocalizer<>), ServiceLifetime.Transient));
        services.AddLogging(builder =>
        {
            if (configuration != null)
            {
                var loggingOptions = configuration.GetSection("Logging");
                builder.AddConfiguration(loggingOptions);

                if (loggingOptions.GetSection(DbLoggerProvider.ProviderName) != null)
                    builder.AddDbLoggerProvider();

                if (loggingOptions.GetSection(FileLoggerProvider.ProviderName) != null)
                    builder.AddFileLoggerProvider();
            }
        });

        services.AddScoped<DataAccess>();
        
        services.AddTransient<SqlServerReadProcedureScripts>();
        services.AddTransient<SqlServerWriteProcedureScripts>();
        services.AddTransient<SqlServerScripts>();
        
        services.AddTransient<EntityProviderBase, SqlServerProvider>();
        services.AddTransient<IEntityRepository, EntityRepository>();
        
        services.AddTransient<IEncryptionAlgorithm, AesEncryptionAlgorithm>();
        services.AddTransient<IEncryptionService,EncryptionService>();

        services.AddSingleton<IBackgroundTaskManager, BackgroundTaskManager>();

        return services;
    }
}
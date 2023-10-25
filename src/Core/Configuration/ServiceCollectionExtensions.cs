﻿using System;
using JJMasterData.Commons.Configuration;
using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Core.Configuration.Options;
using JJMasterData.Core.DataDictionary.Repository;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager.Exportation;
using JJMasterData.Core.DataManager.Exportation.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JJMasterData.Core.Configuration;

public static class ServiceCollectionExtensions
{
    public static MasterDataServiceBuilder AddJJMasterDataCore(this IServiceCollection services)
    {
        services.AddOptions<MasterDataCoreOptions>().BindConfiguration("JJMasterData");
        
        services.AddDefaultServices();
        
        return services.AddJJMasterDataCommons();
    }

    public static MasterDataServiceBuilder AddJJMasterDataCore(this IServiceCollection services,
        Action<MasterDataCoreOptions> configureCore, IConfiguration loggingConfiguration = null)
    {
        var coreOptions = new MasterDataCoreOptions();

        configureCore(coreOptions);

        services.Configure(configureCore);
        
        services.AddDefaultServices();
        return services.AddJJMasterDataCommons(ConfigureJJMasterDataCommonsOptions, loggingConfiguration);

        void ConfigureJJMasterDataCommonsOptions(MasterDataCommonsOptions options)
        {
            options.ConnectionString = coreOptions.ConnectionString;
            options.ConnectionProvider = coreOptions.ConnectionProvider;
            options.LocalizationTableName = coreOptions.LocalizationTableName;
            options.ReadProcedurePattern = coreOptions.ReadProcedurePattern;
            options.WriteProcedurePattern = coreOptions.WriteProcedurePattern;
            options.SecretKey = coreOptions.SecretKey;
        }
    }

    public static MasterDataServiceBuilder AddJJMasterDataCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MasterDataCoreOptions>(configuration.GetJJMasterData());
        
        services.AddDefaultServices();
        
        return services.AddJJMasterDataCommons(configuration);
    }

    private static void AddDefaultServices(this IServiceCollection services)
    {
        
        services.AddHttpServices();
        services.AddDataDictionaryServices();
        services.AddDataManagerServices();
        services.AddEventHandlers();
        services.AddExpressionServices();
        services.AddActionServices();
        
        services.AddScoped<IDataDictionaryRepository, SqlDataDictionaryRepository>();
        
        services.AddScoped<IExcelWriter, ExcelWriter>();
        services.AddScoped<ITextWriter, TextWriter>();
        
        services.AddFactories();
    }
}

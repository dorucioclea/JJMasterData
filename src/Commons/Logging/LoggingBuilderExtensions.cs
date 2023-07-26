using JJMasterData.Commons.Logging.Db;
using JJMasterData.Commons.Logging.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace JJMasterData.Commons.Logging;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddDbLoggerProvider(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, DbLoggerProvider>();
        builder.Services.AddSingleton<DbLoggerBuffer>(_=>new DbLoggerBuffer(1024));
        builder.Services.AddHostedService<DbLoggerBackgroundService>();
        LoggerProviderOptions.RegisterProviderOptions<DbLoggerOptions, DbLoggerProvider>(builder.Services);
        return builder;
    }

    public static ILoggingBuilder AddFileLoggerProvider(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
        builder.Services.AddSingleton<FileLoggerBuffer>(_=>new FileLoggerBuffer(1024));
        builder.Services.AddHostedService<FileLoggerBackgroundService>();
        LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);
        return builder;
    }
}
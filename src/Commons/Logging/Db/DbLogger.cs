#nullable enable

using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace JJMasterData.Commons.Logging.Db;

internal class DbLogger : ILogger
{
    private readonly DbLoggerBuffer _loggerBuffer;

    public DbLogger(DbLoggerBuffer loggerBuffer)
    {
        _loggerBuffer = loggerBuffer;
    }

    public IDisposable BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        
        var now = DateTime.Now;

        var message = GetMessage(eventId, formatter(state, exception), exception);
        
        var entry = new DbLogEntry
        {
            Created = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond,
                now.Kind),
            LogLevel = (int)logLevel,
            Event = eventId.Name ?? string.Empty,
            Message = message
        };
        
        _loggerBuffer.Enqueue(entry.ToSeparatedCharArray());
    }
    
    private static string GetMessage(EventId eventId, string formatterMessage, Exception? exception)
    {
        var message = new StringBuilder();
        message.AppendLine(eventId.Name);
        message.AppendLine(formatterMessage);

        if (exception != null)
        {
            message.AppendLine(LoggerDecoration.GetMessage(exception));
        }

        return message.ToString();
    }
}
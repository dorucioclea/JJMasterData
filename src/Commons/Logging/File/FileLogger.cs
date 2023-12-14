using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;


namespace JJMasterData.Commons.Logging.File;

internal class FileLogger : ILogger
{
    private readonly FileLoggerBuffer _buffer;
    private readonly IOptionsMonitor<FileLoggerOptions> _options;

    /// <summary>
    /// Creates a new instance of <see cref="FileLogger" />.
    /// </summary>
    public FileLogger(FileLoggerBuffer buffer, IOptionsMonitor<FileLoggerOptions> options)
    {
        _buffer = buffer;
        _options = options;
    }

    public IDisposable BeginScope<TState>(TState state) => default!;

    /// <summary>
    /// Whether to log the entry.
    /// </summary>
    /// <param name="logLevel"></param>
    /// <returns></returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    /// <summary>
    /// Used to log the entry.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="logLevel">An instance of <see cref="LogLevel"/>.</param>
    /// <param name="eventId">The event's ID. An instance of <see cref="EventId"/>.</param>
    /// <param name="state">The event's state.</param>
    /// <param name="exception">The event's exception. An instance of <see cref="Exception" /></param>
    /// <param name="formatter">A delegate that formats </param>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = GetMessage(logLevel, eventId, exception, formatter(state, exception));
        var entry = new LogMessage
        {
            Created = DateTime.Now,
            LogLevel = (int)logLevel,
            Event = eventId.Name ?? string.Empty,
            Message = message
        };

        _buffer.Enqueue(entry);
    }

    public string GetMessage(LogLevel logLevel, EventId eventId, Exception exception, string formatterMessage)
    {
        var log = new StringBuilder();
        var formatting = _options.CurrentValue.Formatting;

        switch (formatting)
        {
            case FileLoggerFormatting.Default:

                log.Append(DateTime.Now);
                log.Append(" ");

                log.Append("(");
                log.Append(logLevel.ToString());
                log.AppendLine(")");
                log.Append(formatterMessage);
                log.AppendLine();
                log.AppendLine();
                break;
            case FileLoggerFormatting.Compact:
                {
                    log.AppendFormat("{0:yyyy-MM-dd HH:mm:ss+00:00} -", DateTime.Now);
                    log.AppendFormat(" [{0}] ", logLevel);

                    if (!string.IsNullOrWhiteSpace(eventId.Name))
                    {
                        log.AppendFormat(" [{0}] ", eventId.Name);
                    }

                    log.AppendFormat(" {0} ", formatterMessage);

                    if (exception != null)
                    {
                        log.AppendLine(LoggerDecoration.GetMessageException(exception));
                    }
                    log.AppendLine();
                    break;
                }
            case FileLoggerFormatting.Json:
                log.Append(
                    JsonConvert.SerializeObject(new
                    {
                        Date = DateTime.Now,
                        Event = eventId.Name,
                        logLevel,
                        Message = formatterMessage,
                        exception
                    }, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    }));
                log.AppendLine(",");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(formatting), formatting, null);
        }

        return log.ToString();
    }

}
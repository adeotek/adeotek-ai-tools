using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace AdeotekSqlMcp.Utilities;

/// <summary>
/// Logger factory and configuration
/// </summary>
public static class LoggerFactory
{
    private static Logger? _logger;

    /// <summary>
    /// Creates and configures the global logger
    /// </summary>
    public static Logger CreateLogger(LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        if (_logger != null)
        {
            return _logger;
        }

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "AdeotekSqlMcp")
            .Destructure.ByTransforming<Exception>(ex => new
            {
                Type = ex.GetType().Name,
                ex.Message,
                ex.StackTrace
            })
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Debug
            );

        // Add file logging if LOG_FILE environment variable is set
        var logFile = Environment.GetEnvironmentVariable("LOG_FILE");
        if (!string.IsNullOrEmpty(logFile))
        {
            loggerConfig.WriteTo.File(
                logFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            );
        }

        _logger = loggerConfig.CreateLogger();
        return _logger;
    }

    /// <summary>
    /// Gets the configured logger instance
    /// </summary>
    public static Logger GetLogger()
    {
        return _logger ?? CreateLogger();
    }

    /// <summary>
    /// Sanitizes sensitive data from log messages
    /// </summary>
    public static string SanitizeSensitiveData(string message)
    {
        // Remove password patterns from connection strings
        var patterns = new[]
        {
            @"password\s*=\s*[^;]+",
            @"pwd\s*=\s*[^;]+",
            @"apikey\s*=\s*[^;]+",
            @"token\s*=\s*[^;]+",
            @"secret\s*=\s*[^;]+"
        };

        var sanitized = message;
        foreach (var pattern in patterns)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                pattern,
                "$0=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        return sanitized;
    }
}

/// <summary>
/// Extension methods for logger
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs a sanitized message
    /// </summary>
    public static void LogSanitized(this Logger logger, LogEventLevel level, string messageTemplate, params object[] args)
    {
        var sanitizedMessage = LoggerFactory.SanitizeSensitiveData(messageTemplate);
        logger.Write(level, sanitizedMessage, args);
    }

    /// <summary>
    /// Logs an error with sanitized message
    /// </summary>
    public static void ErrorSanitized(this Logger logger, Exception exception, string messageTemplate, params object[] args)
    {
        var sanitizedMessage = LoggerFactory.SanitizeSensitiveData(messageTemplate);
        logger.Error(exception, sanitizedMessage, args);
    }
}

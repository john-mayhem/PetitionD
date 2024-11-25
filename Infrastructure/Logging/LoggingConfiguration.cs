using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace PetitionD.Infrastructure.Logging;

public static class LoggingConfiguration
{
    public static void ConfigureLogging(ILoggingBuilder builder, string logDirectory)
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        builder
            .ClearProviders()
            .AddConsole()
            .AddDebug()
            .AddEventLog()
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });

        // Add file logging
        var logFilePath = Path.Combine(logDirectory, $"petitiond-{DateTime.UtcNow:yyyy-MM-dd}.log");
        builder.AddProvider(new FileLoggerProvider(logFilePath));
    }
}

// Custom File Logger Implementation
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly object _lock = new object();

    public FileLoggerProvider(string path)
    {
        _path = path;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_path);
    }

    public void Dispose() { }

    private class FileLogger : ILogger
    {
        private readonly string _path;
        private static readonly object Lock = new object();

        public FileLogger(string path)
        {
            _path = path;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {formatter(state, exception)}";
            if (exception != null)
                line += Environment.NewLine + exception.ToString();

            lock (Lock)
            {
                try
                {
                    File.AppendAllText(_path, line + Environment.NewLine);
                }
                catch
                {
                    // Suppress file write errors
                }
            }
        }
    }
}
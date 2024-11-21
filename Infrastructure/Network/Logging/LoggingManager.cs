// File: Infrastructure/Logging/LoggingManager.cs
using Serilog;
using Serilog.Events;

namespace PetitionD.Infrastructure.Logging;

public static class LoggingManager
{
    public static void InitializeLogging(string logDirectory)
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        var logPath = Path.Combine(logDirectory, "petitiond_.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
    }
}
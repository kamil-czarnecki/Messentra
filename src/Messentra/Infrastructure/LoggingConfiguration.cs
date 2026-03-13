using Serilog;

namespace Messentra.Infrastructure;

internal static class LoggingConfiguration
{
    private const string AppName = "Messentra";

    internal static void ConfigureLogging(IHostBuilder host)
    {
        var logFilePath = GetLogFilePath();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateBootstrapLogger();

        host.UseSerilog((_, services, configuration) => configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));
    }

    private static string GetLogFilePath()
    {
        var logDirectory = GetLogDirectory();
        
        Directory.CreateDirectory(logDirectory);
        
        return Path.Combine(logDirectory, "app-.log");
    }

    private static string GetLogDirectory() =>
        OperatingSystem.IsMacOS()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", AppName)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, "logs");
}

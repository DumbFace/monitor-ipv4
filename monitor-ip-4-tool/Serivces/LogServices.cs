using monitor_ip_4_tool.Interfaces;
using Serilog;

namespace monitor_ip_4_tool.Serivces;

public class LogServices : ILog, IDisposable
{
    private readonly ILogger _logger;

    public LogServices()
    {
        _logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo
            .File("logs/log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();
        Console.WriteLine("Log initialized");
    }

    public void Info(string message)
    {
        _logger.Information(message);
    }

    public void Warn(string message)
    {
        _logger.Warning(message);
    }

    public void Error(string message, Exception ex = null)
    {
        if (ex == null)
            _logger.Error(message);
        else
            _logger.Error(ex, message);
    }

    public void Dispose()
    {
        (_logger as IDisposable)?.Dispose();
    }
}
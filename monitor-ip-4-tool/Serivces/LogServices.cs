using System;
using monitor_ip_4_tool.Interfaces;
using Serilog;
using Serilog.Events;

namespace monitor_ip_4_tool.Serivces;

public class LogServices : ILog, IDisposable
{
    private readonly ILogger _logger;
    string path = AppContext.BaseDirectory;
    public LogServices()
    {
        _logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().WriteTo.File($"{path}/logs/log-.txt",
            rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Error).CreateLogger();
        _logger.Information("Logger Starting up");
        _logger.Information($"Path: {path}");

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
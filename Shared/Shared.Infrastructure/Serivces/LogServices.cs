using Serilog;

using Shared.Shared.Common.Interfaces;
namespace Shared.Shared.Infrastructure.Serivces;

public class LogServices : ILog, IDisposable
{
    string path = AppContext.BaseDirectory;
    private readonly ILogger _logger;

    public LogServices(ILogger configuration)
    {
        _logger = configuration;
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

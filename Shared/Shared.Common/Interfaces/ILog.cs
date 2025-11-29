namespace Shared.Shared.Common.Interfaces;

public interface ILog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception ex = null);
}

namespace Shared.Shared.Common.Interfaces;

public interface IInternetProtocol
{
    Task<string> GetIP4Async(CancellationToken token);
}

namespace Shared.Shared.Common.Interfaces
{
    public interface IRetirable
    {
        Task RetryMessageAsync(CancellationToken token = default);
    }
}
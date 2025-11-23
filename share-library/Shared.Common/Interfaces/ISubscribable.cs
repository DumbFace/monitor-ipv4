namespace Shared.Shared.Common.Interfaces
{
    public interface ISubscribable
    {
        Task SubscribeMessageAsync(CancellationToken token = default);
    }
}
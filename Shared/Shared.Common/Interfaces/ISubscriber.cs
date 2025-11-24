using System;

namespace Shared.Shared.Common.Interfaces
{
    public interface ISubscriber
    {

        Task SubscribeAsync(Action handler, CancellationToken token = default);
    }
}
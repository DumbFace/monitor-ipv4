using Polly;
using Shared.Shared.Common.Interfaces;

namespace Shared.Shared.Infrastructure.Serivces
{
    public class RetryServices : IRetryHandler
    {

        private readonly ResiliencePipeline _pipeline;
        public RetryServices(
            IPollyFactory pollyFactory
            )
        {

            _pipeline = pollyFactory.GetPipeLine();
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action)
        {
            return await _pipeline.ExecuteAsync(async token =>
            {
                return await action(token);
            });
        }

        public async Task ExecuteAsync(Func<CancellationToken, Task> action)
        {
            await _pipeline.ExecuteAsync(async token =>
               {
                   await action(token);
               });
        }
    }
}
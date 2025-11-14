
using monitor_ip_4_tool.Interfaces;
using Polly;
using Polly.Registry;

namespace monitor_ip_4_tool.Serivces
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
    }
}
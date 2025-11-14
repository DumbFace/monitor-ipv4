
using monitor_ip_4_tool.Interfaces;
using Polly.Registry;

namespace monitor_ip_4_tool.Serivces
{
    public class RetryServices : IRetryHandler
    {
        private readonly ResiliencePipelineProvider<string> _pipelineProvider;
        private readonly ILog _logger;
        public RetryServices(ResiliencePipelineProvider<string> pipelineProvider, ILog logger)
        {
            _pipelineProvider = pipelineProvider;
            _logger = logger;
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action)
        {
            var pipeline = _pipelineProvider.GetPipeline("defaultPipeline");

            return await pipeline.ExecuteAsync(async token =>
            {
                return await action(token);
            });
        }
    }
}
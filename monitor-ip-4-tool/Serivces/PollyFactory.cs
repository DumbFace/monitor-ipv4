using monitor_ip_4_tool.Interfaces;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace monitor_ip_4_tool.Serivces
{
    public class PollyFactory : IPollyFactory
    {
        private readonly ILog _logger;

        public PollyFactory(ILog logger)
        {
            _logger = logger;
        }

        public ResiliencePipeline GetPipeLine()
        {
            var builder = new ResiliencePipelineBuilder();

            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(5),
                OnRetry = args =>
                {
                    _logger.Warn($"Retry attempt {args.AttemptNumber} after {args.RetryDelay}s due to {args.Outcome.Exception.Message}");
                    return new ValueTask();
                }
            });

            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(10),
                OnTimeout = (args) =>
                {
                    Console.WriteLine($"Timeout {args.Timeout}");
                    return new ValueTask();
                }
            });

            return builder.Build();
        }
    }
}
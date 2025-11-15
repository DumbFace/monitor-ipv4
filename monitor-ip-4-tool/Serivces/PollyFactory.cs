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

        public ResiliencePipeline GetIPServicesPipeLine()
        {
            var builder = new ResiliencePipelineBuilder();

            // builder.AddStrategy<string>();
            builder.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = args =>
                {
                    _logger.Warn($"(string)args.Outcome.Result: {(string)args.Outcome.Result}");
                    return new ValueTask<bool>((string)args.Outcome.Result == string.Empty);
                },
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(3),
                OnRetry = args =>
                {
                    _logger.Warn($"Retry attempt {args.AttemptNumber} after {args.RetryDelay}s due to empty IP");
                    // _logger.Warn($"Retry attempt {args.AttemptNumber} after {args.RetryDelay}s due to {args.Outcome.Exception.Message}");
                    return new ValueTask();
                }
            });
            // builder.AddTimeoutDefault(_logger);

            return builder.Build();
        }


        //Default Pipeline
        public ResiliencePipeline GetPipeLine()
        {
            var builder = new ResiliencePipelineBuilder();

            builder.AddRetryDefault(_logger);
            builder.AddTimeoutDefault(_logger);

            return builder.Build();
        }


    }

    public static class PollyExtension
    {
        public static ResiliencePipelineBuilder AddRetryDefault(this ResiliencePipelineBuilder builder, ILog _logger)
        {
            return builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(5),
                OnRetry = args =>
                {
                    _logger.Warn($"Retry attempt {args.AttemptNumber} after {args.RetryDelay}s due to {args.Outcome.Exception.Message}");
                    return new ValueTask();
                }
            });
        }

        public static ResiliencePipelineBuilder AddTimeoutDefault(this ResiliencePipelineBuilder builder, ILog _logger)
        {
            return builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(15),
                OnTimeout = (args) =>
                {
                    _logger.Warn($"Timeout {args.Timeout}");
                    return new ValueTask();
                }
            });
        }
    }


}

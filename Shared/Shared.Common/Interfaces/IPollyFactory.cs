using Polly;

namespace Shared.Shared.Common.Interfaces
{
    public interface IPollyFactory
    {
        ResiliencePipeline GetPipeLine();

        ResiliencePipeline GetIPServicesPipeLine();

    }
}

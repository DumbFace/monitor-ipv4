using Polly;
using Polly.Registry;
namespace monitor_ip_4_tool.Interfaces
{
    public interface IPollyFactory
    {
        ResiliencePipeline GetPipeLine();
    }
}
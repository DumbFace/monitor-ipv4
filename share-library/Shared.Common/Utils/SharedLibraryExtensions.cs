

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using monitor_ip_4_tool.Constant;
using monitor_ip_4_tool.Models;

namespace Shared.Shared.Common.Utils
{
    public static class SharedLibraryExtensions
    {
        public static IServiceCollection AddSharedLibrary(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.AddOptions<RabbitMqConfig>()
                .Bind(config.GetSection(ConfigEnum.RABBITMQ))
                .ValidateDataAnnotations();
            return services;
        }
    }
}
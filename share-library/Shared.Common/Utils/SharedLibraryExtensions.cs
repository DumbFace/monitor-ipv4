

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Shared.Common.Constant;
using Shared.Shared.Common.Models;

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
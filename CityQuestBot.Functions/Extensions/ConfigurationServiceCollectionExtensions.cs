using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CityQuestBot.Functions
{
    internal static class ConfigurationServiceCollectionExtensions
    {
        public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<Options>(config.GetSection(nameof(Options)));
            return services;
        }
    }
}

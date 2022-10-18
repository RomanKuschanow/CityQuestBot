using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(CityQuestBot.Functions.Startup))]
namespace CityQuestBot.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config =
                new ConfigurationBuilder()
                    .SetBasePath(builder.GetContext().ApplicationRootPath)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

            builder.Services.AddAppConfiguration(config);
        }
    }
}
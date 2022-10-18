using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CityQuestBot.Functions
{
    public class BotUpdate
    {
        private readonly IOptions<Options> options;
        public BotUpdate(IOptions<Options> options)
        {
            this.options = options;
        }

        [FunctionName("BotUpdate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] Update update,
            ILogger log)
        {
            try
            {
                await Bootstrapper.BuildBotUpdateReceiver(options.Value, update, log).HandleUpdate();
            }
            catch (System.Exception ex)
            {
                log.LogError(ex, ex.Message);
            }
            return new OkObjectResult("");
        }
    }
}

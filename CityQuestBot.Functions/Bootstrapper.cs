using Azure.Data.Tables;
using ChannelAdam.Serilog.Sinks.MicrosoftExtensionsLoggingLogger;
using CityQuestBot.Functions.Services;
using Serilog;
using Telegram.Bot.Types;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Azure.Storage.Blobs;

namespace CityQuestBot.Functions
{
    public class Bootstrapper
    {
        private static void RegisterSerilog(ILogger log)
        {
            // Registering Serilog provider
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss.fffffff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.MicrosoftExtensionsLoggingLogger(Serilog.Events.LogEventLevel.Information, log)
                .CreateLogger();
        }

        public static IBotUpdateReceiver BuildBotUpdateReceiver(
            Options options,
            Update update,
            ILogger log)
        {
            RegisterSerilog(log);
            IGetAppSettingService getAppSettingService = new GetAppSettingServiceOnAppSettings(options);

            string conn = getAppSettingService.GetAppSettingOrThrow("AzureWebJobsStorage");
            var usersTableClient = new TableClient(conn, "users");
            var questsTableClient = new TableClient(conn, "quests");
            var messagesTableClient = new TableClient(conn, "messages");
            var answersTableClient = new TableClient(conn, "answers");
            var historyTableClient = new TableClient(conn, "history");
            var blobClient = new BlobContainerClient(conn, "files");

            return new BotUpdateReceiver(
                Log.ForContext<BotUpdateReceiver>(),
                update,
                getAppSettingService,
                usersTableClient,
                questsTableClient,
                messagesTableClient,
                answersTableClient,
                historyTableClient,
                blobClient);
        }
    }
}

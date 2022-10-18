using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using CityQuestBot.Functions.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Serilog;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CityQuestBot.Functions
{
    public class BotUpdateReceiver : IBotUpdateReceiver
    {
        public BotUpdateReceiver(ILogger logger, Update update, IGetAppSettingService getAppSettingService, TableClient tableClient, BlobContainerClient blobClient)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.update = update ?? throw new ArgumentNullException(nameof(update));
            this.tableClient = tableClient;
            this.blobClient = blobClient;
            telegramBotApiKey = getAppSettingService.TryGetAppSetting("TelegramBotApiKey") ?? null;
            botClient = telegramBotApiKey is { } ? new TelegramBotClient(telegramBotApiKey) : null;
        }

        static readonly HttpClient client = new HttpClient();

        private static readonly Regex start = new Regex(@"^/start( [a-fA-F\d]{8}-?[a-fA-F\d]{4}-?[a-fA-F\d]{4}-?[a-fA-F\d]{4}-?[a-fA-F\d]{12})?$");

        public async Task<string> HandleUpdate()
        {
            return "";
        }


        private const string UNKNOWN_COMMAND_REPLY = @"Неизвестная команда";
        private const string NEWLINE = "  \r\n";
        private readonly string? telegramBotApiKey;
        private readonly ILogger logger;
        private readonly TableClient tableClient;
        private readonly BlobContainerClient blobClient;
        private readonly Update update;
        private readonly TelegramBotClient? botClient;
    }
}

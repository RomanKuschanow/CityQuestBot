#nullable disable
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using CityQuestBot.Common;
using CityQuestBot.Common.Models;
using CityQuestBot.Common.Services;
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
        public BotUpdateReceiver(
            ILogger logger, 
            Update update, 
            IGetAppSettingService getAppSettingService,
            TableClient usersTableClient,
            TableClient questsTableClient, 
            TableClient messagesTableClient, 
            TableClient answersTableClient, 
            TableClient historyTableClient, 
            BlobContainerClient clueFilesBlobClient)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.update = update ?? throw new ArgumentNullException(nameof(update));
            this.usersTableClient = usersTableClient;
            this.questsTableClient = questsTableClient;
            this.messagesTableClient = messagesTableClient;
            this.answersTableClient = answersTableClient;
            this.historyTableClient = historyTableClient;
            this.clueFilesBlobClient = clueFilesBlobClient;
            telegramBotApiKey = getAppSettingService.TryGetAppSetting("TelegramBotApiKey") ?? null;
            botClient = telegramBotApiKey is { } ? new TelegramBotClient(telegramBotApiKey) : null;
        }

        static readonly HttpClient client = new HttpClient();

        public async Task<string> HandleUpdate()
        {
            Message message = update.Message;
            Users user = await UsersServices.GetUser(usersTableClient, message.Chat.Id.ToString());

            if (message == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "I only understand text");
            }

            if (message.Text.StartsWith("/start") && string.IsNullOrWhiteSpace(user.CurrentQuest))
            {
                await Handlers.CommandStart(botClient, usersTableClient, questsTableClient, messagesTableClient, answersTableClient, clueFilesBlobClient, update);
                await Handlers.WriteHistory(historyTableClient, usersTableClient, update);
                return "";
            }

            if (message.Text.StartsWith("/editPrewAnswer") && string.IsNullOrWhiteSpace(user.CurrentQuest))
            {
                await Handlers.CommandChangeAnswer(botClient, usersTableClient, answersTableClient, messagesTableClient, update);
                await Handlers.WriteHistory(historyTableClient, usersTableClient, update);
                return "";
            }

            if (!string.IsNullOrWhiteSpace(user.CurrentQuest))
            {
                await Handlers.AnswerHandler(botClient, usersTableClient, messagesTableClient, answersTableClient, questsTableClient, clueFilesBlobClient, update);
                await Handlers.WriteHistory(historyTableClient, usersTableClient, update);
                return "";
            }

            return "";
        }


        private const string UNKNOWN_COMMAND_REPLY = @"Неизвестная команда";
        private const string NEWLINE = "  \r\n";
        private readonly string? telegramBotApiKey;
        private readonly ILogger logger;
        private readonly TableClient usersTableClient;
        private readonly TableClient questsTableClient;
        private readonly TableClient messagesTableClient;
        private readonly TableClient answersTableClient;
        private readonly TableClient historyTableClient;
        private readonly BlobContainerClient clueFilesBlobClient;
        private readonly Update update;
        private readonly TelegramBotClient? botClient;
    }
}

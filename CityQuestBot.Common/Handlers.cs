using Azure.Data.Tables;
using CityQuestBot.Common.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using Azure;
using CityQuestBot.Common.Services;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Azure.Storage.Blobs;
using System.Linq;
using System.Threading;
using Azure.Storage.Blobs.Models;
using Telegram.Bot.Types.InputFiles;
using System.IO;

namespace CityQuestBot.Common
{
    public static class Handlers
    {
        public static async Task CommandStart(
            TelegramBotClient botClient, 
            TableClient usersTableClient, 
            TableClient questsTableClient,
            TableClient messagesTableClient,
            BlobContainerClient blobClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());

            Regex start = new Regex(@"^/start( [a-fA-F\d]{8}-?[a-fA-F\d]{4}-?[a-fA-F\d]{4}-?[a-fA-F\d]{4}-?[a-fA-F\d]{12})?$");

            string text = "";

            if (update.Message.Text == "/start")
            {
                text = "Для участия в игре попросите пригласительную ссылку у организатора";
            }
            else if (start.IsMatch(update.Message.Text))
            {
                var quest = await questsTableClient.GetEntityAsync<Quests>("d", start.Match(update.Message.Text).Value);
                user.CurrentQuest = quest.Value.RowKey;
                user.CurrentStep = 1;
                await usersTableClient.UpdateEntityAsync(user, ETag.All);
                await SendClue(botClient, usersTableClient, messagesTableClient, blobClient, update);
            }
            else
            {
                text = "";
            }

            await botClient.SendTextMessageAsync(chatId, text);
        }

        public static async Task AnswerHandler(
            TelegramBotClient botClient, 
            TableClient usersTableClient,
            TableClient messagesTableClient, 
            TableClient answersTableClient, 
            BlobContainerClient blobClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;
            string text = "";

            if (string.IsNullOrWhiteSpace(update.Message.Text) || !new Regex(@"^\w$").IsMatch(update.Message.Text))
            {
                text = "Сообщение должно содержать текст и исключительно текст";
                await botClient.SendTextMessageAsync(chatId, text);
                return;
            }

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());
            await AnswersServices.CreateAnswer(update.Message.Text, user, answersTableClient);
            user.CurrentStep++;
            await usersTableClient.UpdateEntityAsync(user, ETag.All);

            await SendClue(botClient, usersTableClient, messagesTableClient, blobClient, update);
        }

        private static async Task SendClue(
            TelegramBotClient botClient,
            TableClient usersTableClient,
            TableClient messagesTableClient,
            BlobContainerClient blobClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());

            var messages = await ClueServices.GetClue(messagesTableClient, user.CurrentQuest, user.CurrentStep);
            messages.OrderBy(m => m.Order);

            int minutes = 4;

            foreach (var message in messages)
            {
                if (message.Type == "fact")
                {
                    await Task.Run(async () =>
                    {
                        Thread.Sleep(1000 * 60 * 4);
                        minutes += 4;
                        if (message.ImageId != null)
                        {
                            var blob = blobClient.GetBlobClient(message.ImageId);

                            var file = (await blob.DownloadStreamingAsync()).Value.Content;

                            await botClient.SendPhotoAsync(chatId, new InputOnlineFile(file), caption: message.MessageText);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, message.MessageText);
                        }
                    });

                    continue;
                }

                if (message.ImageId != null)
                {
                    var blob = blobClient.GetBlobClient(message.ImageId);

                    var file = (await blob.DownloadStreamingAsync()).Value.Content;

                    await botClient.SendPhotoAsync(chatId, new InputOnlineFile(file), caption: message.MessageText);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, message.MessageText);
                }

            }
        }
    }
}


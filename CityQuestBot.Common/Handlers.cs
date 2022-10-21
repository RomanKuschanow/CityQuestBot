using Azure.Data.Tables;
using CityQuestBot.Common.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using Azure;
using CityQuestBot.Common.Services;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.Linq;
using System.Threading;
using Azure.Storage.Blobs.Models;
using Telegram.Bot.Types.InputFiles;
using System.IO;
using System;

namespace CityQuestBot.Common
{
    public static class Handlers
    {
        public static async Task CommandStart(
            TelegramBotClient botClient,
            TableClient usersTableClient,
            TableClient questsTableClient,
            TableClient messagesTableClient,
            TableClient answersTableClient,
            BlobContainerClient clueFilesBlobClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());

            Regex start = new Regex(@"^/start ([a-fA-F\d]{8}-?[a-fA-F\d]{4}-?[a-fA-F\d]{4}-?[a-fA-F\d]{4}-?[a-fA-F\d]{12})?$");

            string text = "";

            if (update.Message.Text == "/start")
            {
                text = "To participate in the game, ask the organizer for an invitation link";
            }
            else if (start.IsMatch(update.Message.Text))
            {
                try
                {
                    var quest = await questsTableClient.GetEntityAsync<Quests>("d", start.Match(update.Message.Text).Groups[1].Value);
                    user.CurrentQuest = quest.Value.RowKey;
                    user.CurrentStep = 1;
                    await usersTableClient.UpdateEntityAsync(user, ETag.All);
                    await AnswersServices.DeleteAnswers(user.RowKey, answersTableClient);
                    await SendClue(botClient, usersTableClient, messagesTableClient, answersTableClient, clueFilesBlobClient, update);
                    return;
                }
                catch
                {
                    text = "Error, most likely there is no such quest";
                }
            }
            else
            {
                text = "Invalid link";
            }

            await botClient.SendTextMessageAsync(chatId, text);
        }

        public static async Task CommandChangeAnswer(
            TelegramBotClient botClient,
            TableClient usersTableClient,
            TableClient answersTableClient,
            TableClient messagesTableClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());

            Regex regex = new Regex(@"/edit (.+)");

            if (!regex.IsMatch(update.Message.Text))
            {
                string text = "This command is used like this:\r\n\"/edit newAnswer\"";
                await botClient.SendTextMessageAsync(chatId, text);
                return;
            }

            if (user.CurrentStep == 1)
            {
                string text = "Sorry, can't edit the answer that doesn't exist.";
                await botClient.SendTextMessageAsync(chatId, text);
                return;
            }

            string newAnswer = regex.Match(update.Message.Text).Groups[1].Value;

            if (user.CurrentStep == 0)
            {
                int lastStep = await ClueServices.GetClueCount(messagesTableClient, user.CurrentQuest);
                await AnswersServices.EditAnswer(user.RowKey, answersTableClient, lastStep - 1, newAnswer);
            }
            else
            {
                await AnswersServices.EditAnswer(user.RowKey, answersTableClient, user.CurrentStep - 1, newAnswer);
            }
        }

        public static async Task AnswerHandler(
            TelegramBotClient botClient,
            TableClient usersTableClient,
            TableClient messagesTableClient,
            TableClient answersTableClient,
            TableClient questsTableClient,
            BlobContainerClient clueFilesBlobClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;
            string text = "";

            if (string.IsNullOrWhiteSpace(update.Message.Text))
            {
                text = "The answer must only contain text";
                await botClient.SendTextMessageAsync(chatId, text);
                return;
            }

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());
            await AnswersServices.CreateAnswer(update.Message.Text, user, answersTableClient);
            if (user.CurrentStep == 0)
            {
                await SendFinalMessage(botClient, usersTableClient, messagesTableClient, questsTableClient, clueFilesBlobClient, update);
                user.CurrentQuest = null;
                await usersTableClient.UpdateEntityAsync(user, ETag.All);
                return;
            }
            user.CurrentStep++;
            await usersTableClient.UpdateEntityAsync(user, ETag.All);

            await SendClue(botClient, usersTableClient, messagesTableClient, answersTableClient, clueFilesBlobClient, update);
        }

        private static async Task SendClue(
            TelegramBotClient botClient,
            TableClient usersTableClient,
            TableClient messagesTableClient,
            TableClient answersTableClient,
            BlobContainerClient clueFilesBlobClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());

            var messages = await ClueServices.GetClue(messagesTableClient, user.CurrentQuest, user.CurrentStep);
            messages.OrderBy(m => m.Order);

            int minutes = 4;

            foreach (var message in messages)
            {
                if (message.Type == "final")
                {
                    user.CurrentStep = 0;
                    await usersTableClient.UpdateEntityAsync(user, ETag.All);
                    var answers = await AnswersServices.GetAnswers(user.RowKey, answersTableClient);
                    string text = string.Join(Environment.NewLine, answers.Select(a => a.Answer));
                    await botClient.SendTextMessageAsync(chatId, $"Here's the list of all the answers you gave previously:\n {text}");
                }

                if (message.Type == "win" || message.Type == "lose")
                {
                    continue;
                }

                if (message.Type == "fact")
                {
                    new Thread(async () =>
                    {
                        await Task.Delay(1000 * 60 * minutes);
                        var _user = await UsersServices.GetUser(usersTableClient, chatId.ToString());
                        if (_user.CurrentStep != message.Step || string.IsNullOrWhiteSpace(_user.CurrentQuest))
                        {
                            return;
                        }
                        if (message.ImageId != null)
                        {
                            var blob = clueFilesBlobClient.GetBlobClient(message.ImageId);

                            var file = (await blob.DownloadStreamingAsync()).Value.Content;

                            await botClient.SendPhotoAsync(chatId, new InputOnlineFile(file), caption: message.MessageText);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, message.MessageText);
                        }
                    }).Start();
                    minutes += 4;
                    continue;
                }

                if (message.ImageId != null)
                {
                    var blob = clueFilesBlobClient.GetBlobClient(message.ImageId);

                    var file = (await blob.DownloadStreamingAsync()).Value.Content;

                    await botClient.SendPhotoAsync(chatId, new InputOnlineFile(file), caption: message.MessageText);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, message.MessageText);
                }

            }
        }

        private static async Task SendFinalMessage(
            TelegramBotClient botClient,
            TableClient usersTableClient,
            TableClient messagesTableClient,
            TableClient questsTableClient,
            BlobContainerClient clueFilesBlobClient,
            Update update)
        {
            var chatId = update.Message.Chat.Id;

            Users user = await UsersServices.GetUser(usersTableClient, chatId.ToString());

            var quest = (await questsTableClient.GetEntityAsync<Quests>("d", user.CurrentQuest)).Value;

            var messages = await ClueServices.GetClue(messagesTableClient, user.CurrentQuest, await ClueServices.GetClueCount(messagesTableClient, quest.RowKey));

            user.CurrentQuest = string.Empty;

            await usersTableClient.UpdateEntityAsync(user, ETag.All);

            Messages message;

            if (quest.FinalCode == update.Message.Text)
            {
                message = messages.SingleOrDefault(m => m.Type == "win");
            }
            else
            {
                message = messages.SingleOrDefault(m => m.Type == "lose");
            }

            if (message == null)
            {
                await botClient.SendTextMessageAsync(chatId, "I don't yet have the correct answer 🤷‍♂️  but I'll let you know as soon as I can");
                return;
            }

            if (message.ImageId != null)
            {
                var blob = clueFilesBlobClient.GetBlobClient(message.ImageId);

                var file = (await blob.DownloadStreamingAsync()).Value.Content;

                await botClient.SendPhotoAsync(chatId, new InputOnlineFile(file), caption: message.MessageText);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, message.MessageText);
            }
        }

        public static async Task WriteHistory(
            TableClient historyTableClient,
            TableClient usersTableClient,
            Update update)
        {
            Users user = await UsersServices.GetUser(usersTableClient, update.Message.Chat.Id.ToString());
            await HistoryServices.CreateRecord(historyTableClient, user.RowKey, update.Message);
        }
    }
}


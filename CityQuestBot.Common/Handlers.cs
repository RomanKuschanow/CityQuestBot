﻿using Azure.Data.Tables;
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
            BlobContainerClient clueFilesBlobClient,
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
                await SendClue(botClient, usersTableClient, messagesTableClient, clueFilesBlobClient, update);
            }
            else
            {
                text = "";
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

            Regex regex = new Regex(@"/editPrevAnswer (.+)");

            if (!regex.IsMatch(update.Message.Text))
            {
                string text = "Команда должна содержать новый ответ";
                await botClient.SendTextMessageAsync(chatId, text);
                return;
            }

            if (user.CurrentStep == 1)
            {
                string text = "Предыдущей загадки не было";
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

            if (string.IsNullOrWhiteSpace(update.Message.Text) || !new Regex(@"^\w$").IsMatch(update.Message.Text))
            {
                text = "Сообщение должно содержать текст и исключительно текст";
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

            await SendClue(botClient, usersTableClient, messagesTableClient, clueFilesBlobClient, update);
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
                    await botClient.SendTextMessageAsync(chatId, text);
                }

                if (message.Type == "win" || message.Type == "lose")
                {
                    continue;
                }

                if (message.Type == "fact")
                {
                    await Task.Run(async () =>
                    {
                        Thread.Sleep(1000 * 60 * 4);
                        minutes += 4;
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
                    });

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

            var messages = await ClueServices.GetClue(messagesTableClient, user.CurrentQuest, user.CurrentStep);

            var quest = (await questsTableClient.GetEntityAsync<Quests>("d", user.CurrentQuest)).Value;

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
                await botClient.SendTextMessageAsync(chatId, "Правильный ответ мне пока не известен 🤷‍♂️");
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


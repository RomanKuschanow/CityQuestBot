using Azure.Data.Tables;
using CityQuestBot.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Linq;
using System.Text.RegularExpressions;
using Azure;

namespace CityQuestBot.Common
{
    public static class Handlers
    {
        public static async void CommandStart(TelegramBotClient botClient, TableClient usersTableClient, TableClient questsTableClient, Update update)
        {
            var chatId = update.Message.Chat.Id;

            Users user = null;

            var users = usersTableClient.QueryAsync<Users>(e => e.ChatId == chatId.ToString());

            int i = 0;

            await foreach (var item in users)
            {
                user = item;
                i++;
                break;
            }

            if (i == 0)
            {
                user = new Users
                {
                    PartitionKey = "d",
                    RowKey = Guid.NewGuid().ToString(),
                    ChatId = chatId.ToString(),
                    CurrentQuest = null,
                    CurrentStep = 0
                };

                await usersTableClient.AddEntityAsync(user);
            }

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
                user.CurrentStep = 0;
                await usersTableClient.UpdateEntityAsync(user, ETag.All);
            }
            else
            {
                text = "";
            }

            await botClient.SendTextMessageAsync(chatId, text);
        }
    }
}

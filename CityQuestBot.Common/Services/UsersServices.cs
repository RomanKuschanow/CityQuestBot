using Azure.Data.Tables;
using CityQuestBot.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CityQuestBot.Common.Services
{
    public static class UsersServices
    {
        public static async Task<Users> GetUser(TableClient usersTableClient, string chatId)
        {
            var users = usersTableClient.QueryAsync<Users>(u => u.ChatId == chatId);
            Users user = null;

            await foreach (var item in users)
            {
                user = item;
                break;
            }

            if (user == null)
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

            return user;
        }
    }
}

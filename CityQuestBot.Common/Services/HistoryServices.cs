using Azure.Data.Tables;
using CityQuestBot.Common.Models;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CityQuestBot.Common.Services
{
    public static class HistoryServices
    {
        public static async Task CreateRecord(TableClient historyTableClient, string user, Message message)
        {
            var history = new History 
            { 
                PartitionKey = "d",
                RowKey = Guid.NewGuid().ToString(),
                Chat = user,
                MessageId = message.MessageId,
                UserId = message.From.Id,
                UserName = message.From.Username,
                Message = message.Text,
                Time = message.Date
            };

            await historyTableClient.AddEntityAsync(history);
        }
    }
}

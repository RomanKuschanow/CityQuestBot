using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using CityQuestBot.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityQuestBot.Common.Services
{
    public static class ClueServices
    {
        public static async Task<List<Messages>> GetClue(TableClient messagesTableClient, string questId, int step)
        {
            List<Messages> messages = new List<Messages>();

            var messagesFromTable = messagesTableClient.QueryAsync<Messages>(m => m.PartitionKey == questId && m.Step == step);

            await foreach (var message in messagesFromTable)
            {
                messages.Add(message);
            }

            return messages;
        }

        public static async Task<int> GetClueCount(TableClient messagesTableClient, string questId)
        {
            var clue = messagesTableClient.QueryAsync<Messages>(m => m.PartitionKey == questId);
            List<Messages> messages = new List<Messages>();

            await foreach (var message in clue)
            {
                messages.Add(message);
            }

            return messages.OrderBy(m => m.Step).Last().Step;
        }
    }
}

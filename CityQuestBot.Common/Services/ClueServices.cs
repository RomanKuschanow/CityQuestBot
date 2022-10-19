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
    }
}

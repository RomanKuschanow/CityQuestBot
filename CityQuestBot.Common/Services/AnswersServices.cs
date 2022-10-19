using Azure.Data.Tables;
using CityQuestBot.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CityQuestBot.Common.Services
{
    public static class AnswersServices
    {
        public static async Task CreateAnswer(string answer, Users user, TableClient answersTableClient)
        {
            var userAnswer = new UserAnswers
            {
                PartitionKey = user.RowKey,
                RowKey = Guid.NewGuid().ToString(),
                Step = user.CurrentStep,
                QuestId = user.CurrentQuest,
                Answer = answer
            };

            await answersTableClient.AddEntityAsync(userAnswer);
        }
    }
}

using Azure;
using Azure.Data.Tables;
using CityQuestBot.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static async Task<List<UserAnswers>> GetAnswers(string userId, TableClient answersTableClient)
        {
            var answersRequest = answersTableClient.QueryAsync<UserAnswers>(a => a.PartitionKey == userId);
            List<UserAnswers> answers = new List<UserAnswers>();

            await foreach (var answer in answersRequest)
            {
                answers.Add(answer);
            }

            return answers.OrderBy(a => a.Step).ToList();
        }

        public static async Task<UserAnswers> GetAnswer(string userId, TableClient answersTableClient, int step)
        {
            var answers = await GetAnswers(userId, answersTableClient);

            return answers.Single(a => a.Step == step);
        }

        public static async Task EditAnswer(string userId, TableClient answersTableClient, int step, string newAnswer)
        {
            var answer = await GetAnswer(userId, answersTableClient, step);
            answer.Answer = newAnswer;

            await answersTableClient.UpdateEntityAsync(answer, ETag.All);
        }

        public static async Task DeleteAnswers(string userId, TableClient answersTableClient)
        {
            var answers = await GetAnswers(userId, answersTableClient);

            foreach (var answer in answers)
            {
                await answersTableClient.DeleteEntityAsync(userId, answer.RowKey);
            }
        }
    }
}

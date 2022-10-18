using Azure.Data.Tables;
using Azure;
using System;

namespace CityQuestBot.Common.Models
{
    public class UserAnswers : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int Step { get; set; }
        public string QuestId { get; set; }
        public string Answer { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

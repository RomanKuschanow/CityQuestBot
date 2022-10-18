using Azure;
using Azure.Data.Tables;
using System;

namespace CityQuestBot.Common.Models
{
    public class Users : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string ChatId { get; set; }
        public string CurrentQuest { get; set; }
        public int CurrentStep { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

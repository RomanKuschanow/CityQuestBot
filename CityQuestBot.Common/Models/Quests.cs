using Azure.Data.Tables;
using Azure;
using System;

namespace CityQuestBot.Common.Models
{
    public class Quests : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string FinalCode { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

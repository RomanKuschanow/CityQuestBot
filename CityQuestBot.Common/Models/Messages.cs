using Azure.Data.Tables;
using Azure;
using System;

namespace CityQuestBot.Common.Models
{
    public class MyEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int Step { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public string MessageText { get; set; }
        public string ImageId { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

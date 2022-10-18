#nullable disable
using Azure;
using Azure.Data.Tables;
using System;

namespace CityQuestBot.Functions.Data
{
    public class MyEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

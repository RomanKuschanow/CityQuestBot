using Azure.Data.Tables;
using Azure;
using System;

namespace CityQuestBot.Common.Models
{
    public class History : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Chat { get; set; }
        public long MessageId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public string FileId { get; set; }
        public DateTime Time { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}

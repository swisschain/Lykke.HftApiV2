using Lykke.AzureStorage.Tables;

namespace Lykke.HftApi.Services.Idempotency
{
    public class IdempotentEntity : AzureTableEntity
    {
        public static string GetPartitionKey(string referenceId) => referenceId;
        public static string GetRowKey(string referenceId) => referenceId;
        public string Payload { set; get; }

        public static IdempotentEntity Build(string referenceId, string payload=default)
        {
            return new IdempotentEntity
            {
                PartitionKey = GetPartitionKey(referenceId),
                RowKey = GetPartitionKey(referenceId),
                Payload = payload
            };
        }
    }
}

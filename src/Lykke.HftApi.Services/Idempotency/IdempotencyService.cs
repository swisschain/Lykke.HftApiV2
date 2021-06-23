using System.Threading.Tasks;
using AzureStorage;

namespace Lykke.HftApi.Services.Idempotency
{
    public class IdempotencyService
    {
        private readonly INoSQLTableStorage<IdempotentEntity> _tableStorage;

        public IdempotencyService(INoSQLTableStorage<IdempotentEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<string> CreateEntityOrGetPayload(string requestId, string payload)
        {
            var entity = IdempotentEntity.Build(requestId, payload);
            
            var createdNow = await _tableStorage.CreateIfNotExistsAsync(entity);

            if (createdNow)
            {
                return null;
            }
            else
            {
                entity = await _tableStorage.GetTopRecordAsync(entity.PartitionKey);

                return entity.Payload;
            }
        }
    }
}

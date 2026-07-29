using System.Text.Json;
using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2.Services
{
    public interface ISpinWebhookPersistenceService
    {
        Task<int> SavePayloadAsync(SpinWebhookPayload payload, string rawBody, DateTime receivedAt);

        Task NormalizeExistingPayloadAsync(int payloadId, JsonElement jsonData);
    }
}

using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2
{
    public interface IAuthService
    {
        Task<bool>                  ValidateClientAsync(string clientId, string clientSecret, string entityId);
        Task<TokenResponse>         GenerateTokenAsync(string clientId, string entityId);
        Task<CreateClientResponse>  CreateClientAsync(string entityId, string clientName);
    }
}

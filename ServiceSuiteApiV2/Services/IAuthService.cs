using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2
{
    public interface IAuthService
    {
        Task<bool>          ValidateClientAsync(string clientId, string clientSecret,string EntityId);
        Task<TokenResponse> GenerateTokenAsync(string clientId,string EntityId);
    }
}

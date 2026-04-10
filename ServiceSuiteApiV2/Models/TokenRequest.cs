namespace ServiceSuiteApiV2.Models
{
    /// <summary>Request body for POST /auth/token</summary>
    public class TokenRequest
    {
        public string ClientId     { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public int EntityId { get; set; } 
    }
}

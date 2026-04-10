namespace ServiceSuiteApiV2.Models
{
    /// <summary>Response returned from POST /auth/token</summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string TokenType   { get; set; } = "Bearer";
        public int    ExpiresIn   { get; set; }  // seconds
    }
}

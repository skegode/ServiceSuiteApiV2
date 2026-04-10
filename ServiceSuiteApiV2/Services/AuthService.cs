using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;

        public AuthService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Checks dbo.ApiClients to confirm the ClientId and ClientSecret are valid.
        /// </summary>
        public async Task<bool> ValidateClientAsync(string clientId, string clientSecret ,string EntityId)
        {
            string connStr = _config.GetConnectionString("DefaultConnection")!;

            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            const string sql = """
                SELECT COUNT(1)
                FROM dbo.ApiClients
                WHERE ClientId = @ClientId
                  AND ClientSecret = @ClientSecret
                  AND EntityId=@EntityId
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientId", clientId);
            cmd.Parameters.AddWithValue("@ClientSecret", clientSecret);
            cmd.Parameters.AddWithValue("@EntityId", EntityId);

            var count = (int)await cmd.ExecuteScalarAsync()!;
            return count > 0;
        }


        public Task<TokenResponse> GenerateTokenAsync(string clientId, string entityId)
        {
            int expiryMinutes = _config.GetValue<int>("Jwt:ExpiryMinutes", 60);
            string secret = _config["Jwt:Secret"]!;

            // Use UTF8 bytes — must match how Program.cs reads the key
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            // ── Include EntityId in the Claims ────────────────────────
            var claims = new[]
            {
        new Claim("client_id", clientId),
        new Claim("EntityId", entityId), // <--- Added this
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Task.FromResult(new TokenResponse
            {
                AccessToken = tokenString,
                TokenType = "Bearer",
                ExpiresIn = expiryMinutes * 60
            });
        }
    }
}
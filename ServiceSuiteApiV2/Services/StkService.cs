using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using ServiceSuiteApiV2.Helpers;
using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2
{
    public class StkService : IStkService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public StkService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> InitiateStkPushAsync(
            decimal amount,
            string phoneNumber,
            int entityId,
            string? accountReference = null,
            string transactionDesc = "Payment")
        {
            var constr = _config.GetConnectionString("DefaultConnection")!;
            var d = new Decipher();
            var stkParams = await GetStkParams(entityId, constr);
            if (stkParams == null)
                return "Failed to retrieve STK parameters for the given EntityId.";

            string consumerKey    = d.Decripting(stkParams.ConsumerKey).Trim();
            string consumerSecret = d.Decripting(stkParams.ConsumerSecrete).Trim();
            string callbackUrl    = d.Decripting(stkParams.CallBackUrl).Trim();
            string passkey        = d.Decripting(stkParams.passkey).Trim();
            string shortcode      = d.Decripting(stkParams.shortCode).Trim();
            string baseUrl        = stkParams.BaseUrl?.Trim().TrimEnd('/') ?? "https://api.safaricom.co.ke";

            string token = await GetOAuthTokenAsync(baseUrl, consumerKey, consumerSecret);
            if (string.IsNullOrEmpty(token))
                return "Failed to obtain OAuth token.";

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string password  = Convert.ToBase64String(Encoding.UTF8.GetBytes(shortcode + passkey + timestamp));
            string phone     = FormatPhone(phoneNumber);

            var payload = new
            {
                BusinessShortCode = shortcode,
                Password          = password,
                Timestamp         = timestamp,
                TransactionType   = "CustomerPayBillOnline",
                Amount            = (int)Math.Ceiling(amount),
                PartyA            = phone,
                PartyB            = shortcode,
                PhoneNumber       = phone,
                CallBackURL       = callbackUrl,
                AccountReference  = accountReference ?? entityId.ToString(),
                TransactionDesc   = transactionDesc
            };

            try
            {
                var http    = _httpClientFactory.CreateClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var content  = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await http.PostAsync($"{baseUrl}/mpesa/stkpush/v1/processrequest", content);
                var body     = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[STK] Status={(int)response.StatusCode} Body={body}");
                return body;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STK] Exception: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<string> GetOAuthTokenAsync(string baseUrl, string consumerKey, string consumerSecret)
        {
            try
            {
                var http        = _httpClientFactory.CreateClient();
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{consumerKey}:{consumerSecret}"));
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                var response    = await http.GetAsync($"{baseUrl}/oauth/v1/generate?grant_type=client_credentials");
                var body        = await response.Content.ReadAsStringAsync();
                using var doc   = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("access_token").GetString() ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STK] OAuth error: {ex.Message}");
                return string.Empty;
            }
        }

        private static async Task<StkParamsDto?> GetStkParams(int entityId, string constr)
        {
            try
            {
                await using var conn = new SqlConnection(constr);
                const string sql = """
                    SELECT TOP 1 ConsumerKey, ConsumerSecrete, CallBackUrl, passkey, shortCode, BaseUrl
                    FROM Transactions.dbo.StkParams
                    WHERE EntityId = @EntityId
                    """;
                return await conn.QueryFirstOrDefaultAsync<StkParamsDto>(sql, new { EntityId = entityId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STK] GetStkParams error: {ex.Message}");
                return null;
            }
        }

        private static string FormatPhone(string phone)
        {
            phone = phone.Trim().Replace(" ", "").Replace("-", "");
            if (phone.StartsWith("0") && phone.Length == 10)
                return "254" + phone[1..];
            if (phone.StartsWith("+254"))
                return phone[1..];
            return phone;
        }
    }
}

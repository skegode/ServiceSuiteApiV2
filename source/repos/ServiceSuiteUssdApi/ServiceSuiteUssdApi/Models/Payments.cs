using System.Text;
using Newtonsoft.Json;
using RestSharp;
using Dapper;
using System.Data.SqlClient;

namespace ServiceSuiteUssdApi.Models
{
    public class Payments
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> InitiateStkPush(decimal amount, string phoneNumber, int entityId, string constr)
        {
            Decipher d = new Decipher();
            var stkParams = await GetStkParams(entityId, constr);
            if (stkParams == null)
                return "Failed to retrieve STK parameters for the given EntityId.";

            string consumerKey    = d.Decripting(stkParams.ConsumerKey).Trim();
            string consumerSecret = d.Decripting(stkParams.ConsumerSecrete).Trim();
            string callbackUrl    = d.Decripting(stkParams.CallBackUrl).Trim();
            string passkey        = d.Decripting(stkParams.passkey).Trim();
            string shortcode      = d.Decripting(stkParams.shortCode).Trim();

            // BaseUrl is stored as plain text (not encrypted), unlike the other STK params.
            string baseUrl = string.IsNullOrWhiteSpace(stkParams.BaseUrl as string)
                             ? "https://api.safaricom.co.ke"
                             : (stkParams.BaseUrl as string).Trim().TrimEnd('/');

            string stkPushEndpoint = $"{baseUrl}/mpesa/stkpush/v1/processrequest";
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string token = await GetSafaricomTokenAsync(consumerKey, consumerSecret, baseUrl);
            string requestBody = BuildStkRequestBody(phoneNumber, amount, callbackUrl, timestamp, passkey, shortcode, phoneNumber);

            Console.WriteLine($"[STK] EntityId={entityId} BaseUrl={baseUrl} Shortcode={shortcode}");
            Console.WriteLine($"[STK] Token={token}");
            Console.WriteLine($"[STK] RequestBody={requestBody}");

            using var request = new HttpRequestMessage(HttpMethod.Post, stkPushEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                using var httpResponse = await _httpClient.SendAsync(request);
                string responseBody = await httpResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[STK] StatusCode={(int)httpResponse.StatusCode} Response={responseBody}");
                return responseBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STK] Exception: {ex.Message}");
                return string.Empty;
            }
        }

        public static async Task<string> GetSafaricomTokenAsync(string consumerKey, string consumerSecret,
            string baseUrl = "https://api.safaricom.co.ke")
        {
            var restClient = new RestClient(baseUrl);
            var request = new RestRequest("/oauth/v1/generate", Method.Get);
            request.AddParameter("grant_type", "client_credentials", ParameterType.GetOrPost);

            var authBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{consumerKey}:{consumerSecret}"));
            request.AddHeader("Authorization", $"Basic {authBase64}");

            var response = await restClient.ExecuteAsync<SafaricomTokenResponse>(request);

            if (response.IsSuccessful)
            {
                TokenResponse receivedData = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
                return receivedData.access_token;
            }

            throw new Exception($"Failed to generate Safaricom API token: {response.ErrorMessage}");
        }

        private string BuildStkRequestBody(string phoneNumber, decimal amount, string callbackUrl,
            string timestamp, string passkey, string shortcode, string accountReference)
        {
            var requestBody = new
            {
                BusinessShortCode = shortcode,
                Password = GetPassword(timestamp, passkey, shortcode),
                Timestamp = timestamp,
                TransactionType = "CustomerPayBillOnline",
                Amount = amount.ToString(),
                PartyA = phoneNumber,
                PartyB = shortcode,
                PhoneNumber = phoneNumber,
                CallBackURL = callbackUrl,
                AccountReference = accountReference,
                TransactionDesc = "services"
            };

            return JsonConvert.SerializeObject(requestBody);
        }

        private string GetPassword(string timestamp, string passkey, string shortCode)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes($"{shortCode}{passkey}{timestamp}");
            return Convert.ToBase64String(plainTextBytes);
        }

        private async Task<dynamic> GetStkParams(int entityId, string constr)
        {
            using (var connection = new SqlConnection(constr))
            {
                return await connection.QueryFirstOrDefaultAsync(
                    "EXEC [dbo].[sp_GetUssdStkParams] @EntityId", new { EntityId = entityId });
            }
        }
    }
}

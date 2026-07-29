using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceSuiteApiV2.Models
{
    public class SpinWebhookPayload
    {
        [JsonPropertyName("remote_identifier")]
        public string? RemoteIdentifier { get; set; }

        [JsonPropertyName("file_unique_id")]
        public string FileUniqueId { get; set; } = "";

        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = "";

        [JsonPropertyName("filename")]
        public string? FileName { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        // MPESA-specific
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("id_number")]
        public string? IdNumber { get; set; }

        // Bank-specific
        [JsonPropertyName("bank_name")]
        public string? BankName { get; set; }

        [JsonPropertyName("account_number")]
        public string? AccountNumber { get; set; }

        [JsonPropertyName("duration")]
        public decimal? Duration { get; set; }

        [JsonPropertyName("json_data")]
        public JsonElement? JsonData { get; set; }

        [JsonPropertyName("timestamp")]
        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("state_name")]
        public string? StateName { get; set; }
    }
}

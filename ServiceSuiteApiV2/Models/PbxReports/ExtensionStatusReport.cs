using System.Text.Json.Serialization;

namespace ServiceSuiteApiV2.Models.PbxReports
{
    public class ExtensionStatusReport
    {
        [JsonPropertyName("extension")]
        public string? Extension { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("registeredIP")]
        public string? RegisteredIP { get; set; }

        [JsonPropertyName("sn")]
        public string? Sn { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace ServiceSuiteApiV2.Models.PbxReports
{
    public class CallFailureReport
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("callid")]
        public string? CallId { get; set; }

        [JsonPropertyName("outbound")]
        public OutboundCall? Outbound { get; set; }

        [JsonPropertyName("ext")]
        public ExtDetails? Ext { get; set; }

        [JsonPropertyName("sn")]
        public string? Sn { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace ServiceSuiteApiV2.Models.PbxReports
{
    public class CallReport
    {
        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("callid")]
        public string? CallId { get; set; }

        [JsonPropertyName("call")]
        public List<CallDetail>? Call { get; set; }

        [JsonPropertyName("sn")]
        public string? Sn { get; set; }

        [JsonPropertyName("info")]
        public string? Info { get; set; }

        [JsonPropertyName("infos")]
        public string? Infos { get; set; }

        [JsonPropertyName("flag")]
        public string? Flag { get; set; }
    }

    public class CallDetail
    {
        [JsonPropertyName("ext")]
        public ExtDetails? Ext { get; set; }

        [JsonPropertyName("inbound")]
        public InboundCall? Inbound { get; set; }

        [JsonPropertyName("outbound")]
        public OutboundCall? Outbound { get; set; }
    }

    public class ExtDetails
    {
        [JsonPropertyName("extid")]
        public string? ExtId { get; set; }
    }

    public class InboundCall
    {
        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("to")]
        public string? To { get; set; }

        [JsonPropertyName("trunk")]
        public string? Trunk { get; set; }

        [JsonPropertyName("inboundid")]
        public string? InboundId { get; set; }
    }

    public class OutboundCall
    {
        [JsonPropertyName("from")]
        public string? From { get; set; }

        [JsonPropertyName("to")]
        public string? To { get; set; }

        [JsonPropertyName("outboundid")]
        public string? OutboundId { get; set; }

        [JsonPropertyName("trunk")]
        public string? Trunk { get; set; }
    }
}

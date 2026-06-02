namespace ServiceSuiteApiV2.Models
{
    public class StkParamsDto
    {
        public string ConsumerKey { get; set; } = "";
        public string ConsumerSecrete { get; set; } = "";
        public string CallBackUrl { get; set; } = "";
        public string passkey { get; set; } = "";
        public string shortCode { get; set; } = "";
        public string? BaseUrl { get; set; }
    }
}

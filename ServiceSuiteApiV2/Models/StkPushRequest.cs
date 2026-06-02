namespace ServiceSuiteApiV2.Models
{
    public class StkPushRequest
    {
        public decimal Amount { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string? AccountReference { get; set; }
        public string? TransactionDesc { get; set; }
    }
}

namespace ServiceSuiteApiV2.Models
{
    public class WriteoffRequest
    {
        public string LoanId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string EntityId { get; set; } = "";
        public decimal CurrentOlb { get; set; }
        public string Reason { get; set; } = "";
        public string? Ref { get; set; }
    }
}

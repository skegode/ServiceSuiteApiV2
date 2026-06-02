namespace ServiceSuiteApiV2.Models
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public string TransId { get; set; } = "";
        public decimal TransAmount { get; set; }
        public string BillRefNumber { get; set; } = "";
        public string PayerName { get; set; } = "";
        public DateTime? DateDone { get; set; }
        public int IsPosted { get; set; }
        public string TransactionType { get; set; } = "";
        public string? LoanId { get; set; }
    }
}

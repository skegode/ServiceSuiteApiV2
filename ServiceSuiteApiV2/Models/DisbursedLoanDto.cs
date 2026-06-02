namespace ServiceSuiteApiV2.Models
{
    public class DisbursedLoanDto
    {
        public string LoanId { get; set; } = "";
        public string BorrowerId { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public decimal LoanAmount { get; set; }
        public decimal AmountToDisburse { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public string ProductName { get; set; } = "";
        public decimal CurrentBalance { get; set; }
    }
}

namespace ServiceSuiteApiV2.Models
{
    public class BorrowerStatementLineDto
    {
        public int Id { get; set; }
        public string LoanId { get; set; } = "";
        public decimal Amount { get; set; }
        public int TransType { get; set; }
        public string Narration { get; set; } = "";
        public string MpesaRef { get; set; } = "";
        public decimal LoanBalance { get; set; }
        public decimal AccountBalance { get; set; }
        public DateTime TransactedDate { get; set; }
    }

    public class BorrowerStatementDto
    {
        public string BorrowerId { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public int TotalLines { get; set; }
        public List<BorrowerStatementLineDto> Statement { get; set; } = new();
    }
}

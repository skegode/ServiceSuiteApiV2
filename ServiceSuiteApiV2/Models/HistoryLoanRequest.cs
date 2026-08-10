namespace ServiceSuiteApiV2.Models
{
    public class HistoryLoanRequest
    {
        public string EntityId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string BorrowerId { get; set; } = "";
        public decimal Principal { get; set; }
        public string ProductId { get; set; } = "";
        public string? GuarantorId { get; set; }
        public decimal? ActualAssetPrice { get; set; }
        public DateTime BorrowDate { get; set; }
        public string TransactionRef { get; set; } = "";
        public int? SelectedPeriod { get; set; }
        public string? SelectedOptionalFeeIds { get; set; }
    }

    public class HistoryLoanResultDto
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string LoanId { get; set; } = "";
        public int Code { get; set; }
        public string Response { get; set; } = "";
    }
}

namespace ServiceSuiteApiV2.Models
{
    public class NewBorrowerRequest
    {
        public string EntityId { get; set; } = "";
        public string UserId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string OtherName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string? NationalID { get; set; }
        public string? EmailAddress { get; set; }
        public string? AccountNo { get; set; }
        public string? PostalAddress { get; set; }
        public string? PhysicalAddress { get; set; }
        public DateTime? DOB { get; set; }
        public string? Gender { get; set; }
        public decimal? CreditScore { get; set; }
        public decimal? LoanLimit { get; set; }
    }

    public class NewBorrowerResultDto
    {
        public int BorrowerId { get; set; }
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
        public string Message { get; set; } = "";
    }
}

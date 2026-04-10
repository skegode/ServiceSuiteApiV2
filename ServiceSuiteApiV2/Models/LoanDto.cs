namespace ServiceSuiteApiV2.Models
{
    public class LoanDto
    {
        public string Id { get; set; } = "";

        public string BorrowerId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string OtherName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string NationalId { get; set; } = "";
        public decimal AmountToDisburse { get; set; }
        public string RepaymentPeriod { get; set; } = "";
        public decimal Arrears { get; set; }
        public int DaysInArrears { get; set; }
        public decimal LoanBalance { get; set; }
        public string Branch { get; set; } = "";
        public decimal OutsourcedAmount { get; set; }
        public decimal Penalty { get; set; }
        public string ProductName { get; set; } = "";
        public string Agent { get; set; } = "";
        public string AgentId { get; set; } = "";
    }
}
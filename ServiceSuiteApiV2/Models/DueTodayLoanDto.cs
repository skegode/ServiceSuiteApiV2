namespace ServiceSuiteApiV2.Models
{
    public class DueTodayLoanDto
    {
        public string LoanId         { get; set; } = "";
        public string FirstName      { get; set; } = "";
        public string OtherName      { get; set; } = "";
        public string PhoneNumber    { get; set; } = "";
        public string EmailAddress   { get; set; } = "";
        public string NationalId     { get; set; } = "";
        public decimal AmountToDisburse { get; set; }
        public decimal LoanBalance   { get; set; }
        public decimal DueTodayAmount { get; set; }
        public string ProductName    { get; set; } = "";
        public DateTime DueDate      { get; set; }
    }
}

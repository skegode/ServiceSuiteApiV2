namespace ServiceSuiteApiV2.Models
{
    public class BorrowerDto
    {
        public string BorrowerId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string OtherName { get; set; } = "";
        public string NationalID { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string EmailAddress { get; set; } = "";
        public string AccountNo { get; set; } = "";
        public int AccountStatus { get; set; }
    }
}

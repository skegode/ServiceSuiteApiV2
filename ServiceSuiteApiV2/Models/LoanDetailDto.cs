namespace ServiceSuiteApiV2.Models
{
    public class LoanDetailDto
    {

        public int Id { get; set; } // Added ID column
        public string LoanId { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string ItemValue { get; set; } = "";
    }
}

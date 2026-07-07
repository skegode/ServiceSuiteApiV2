namespace ServiceSuiteApiV2.Models
{
    public class ClientProfileDto
    {
        public BorrowerDto Client { get; set; } = new();
        public List<LoanDto> ActiveLoans { get; set; } = new();
    }
}

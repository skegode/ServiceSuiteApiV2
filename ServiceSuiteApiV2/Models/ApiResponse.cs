namespace ServiceSuiteApiV2.Models
{
    /// <summary>Generic API response wrapper</summary>
    public class ApiResponse<T>
    {
        public bool   Success { get; set; }
        public string Message { get; set; } = "";
        public T?     Data    { get; set; }
    }

    /// <summary>Loan list response with count</summary>
    public class LoanResponse
    {
        public int           Count { get; set; }
        public List<LoanDto> Data  { get; set; } = new();
    }
}

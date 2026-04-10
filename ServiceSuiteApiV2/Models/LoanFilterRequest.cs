namespace ServiceSuiteApiV2.Models
{
    /// <summary>
    /// Request body for POST /loans.
    /// All fields are optional — null means no filter applied for that parameter.
    /// </summary>
    public class LoanFilterRequest
    {
        public int?     MinDays   { get; set; }   // Min days past due
        public int?     MaxDays   { get; set; }   // Max days past due
        public decimal? MinAmount { get; set; }   // Min original loan amount
        public decimal? MaxAmount { get; set; }   // Max original loan amount
        public decimal? MinOlb    { get; set; }   // Min outstanding loan balance
        public decimal? MaxOlb    { get; set; }   // Max outstanding loan balance
        public string EntityId { get; set; } = "";
    }
}

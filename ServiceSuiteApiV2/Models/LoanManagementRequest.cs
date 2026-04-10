namespace ServiceSuiteApiV2.Models
{
    public class LoanManagementRequest
    {
        // 1. Mandatory Security Context (Added)
        // This is injected by the Controller from the JWT to ensure 
        // a user only manages loans within their own company/entity.
        public string EntityId { get; set; } = "";

        // 2. Identity Info (Updated to string if using Guid/IdentityUser IDs)
        public string UserId { get; set; } = "";

        // 3. Action Values
        public decimal Value { get; set; }
        public int ValueType { get; set; } // e.g., 1 for Percentage, 2 for Fixed Amount
        public string LoanIds { get; set; } = ""; // Comma-separated IDs (e.g., "L001,L002")

        // 4. Transaction Details
        public int TransType { get; set; } // e.g., 1 for Waiver, 2 for Penalty reversal
        public string? Narration { get; set; }

        // 5. Workflow Control (Changed to string to match 'Approve' logic in service)
        public string ActionType { get; set; } = "Approve";
    }
}
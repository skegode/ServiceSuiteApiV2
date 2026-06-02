namespace ServiceSuiteApiV2.Models
{
    public class FraudSummary
    {
        public int HighRiskAgentCount { get; set; }
        public int HighRiskShopCount { get; set; }
        public int DuplicateIdentityCount { get; set; }
        public int LoanStackingCount { get; set; }
        public int ZeroPaymentLoanCount { get; set; }
        public int UnpostedPaymentCount { get; set; }
        public decimal UnpostedPaymentAmount { get; set; }
        public decimal EstimatedFraudExposure { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class AgentFraudSignal
    {
        public string AgentId { get; set; } = "";
        public string AgentName { get; set; } = "";
        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public int DefaultedLoans { get; set; }
        public decimal DefaultRate { get; set; }
        public decimal TotalArrears { get; set; }
        public decimal TotalDisbursed { get; set; }
        public int WriteoffCount { get; set; }
        public decimal TotalWrittenOff { get; set; }
        public string RiskLevel { get; set; } = "Low";
        public List<string> Flags { get; set; } = new();
    }

    public class ShopFraudSignal
    {
        public string AgentId { get; set; } = "";
        public string AgentName { get; set; } = "";
        public int UniqueBorrowers { get; set; }
        public int DefaultedBorrowers { get; set; }
        public decimal BorrowerDefaultRate { get; set; }
        public decimal TotalDisbursed { get; set; }
        public decimal TotalArrears { get; set; }
        public decimal PortfolioAtRisk { get; set; }
        public string RiskLevel { get; set; } = "Low";
    }

    public class DuplicateIdentitySignal
    {
        public string NationalId { get; set; } = "";
        public int BorrowerCount { get; set; }
        public string BorrowerIds { get; set; } = "";
        public string Names { get; set; } = "";
        public string PhoneNumbers { get; set; } = "";
        public decimal TotalActiveBalance { get; set; }
    }

    public class LoanStackingSignal
    {
        public string BorrowerId { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string AgentName { get; set; } = "";
        public int ActiveLoanCount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalDisbursed { get; set; }
        public DateTime? FirstLoanDate { get; set; }
        public DateTime? LatestLoanDate { get; set; }
    }

    public class SuspiciousLoanSignal
    {
        public string LoanId { get; set; } = "";
        public string BorrowerId { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string NationalId { get; set; } = "";
        public string AgentId { get; set; } = "";
        public string AgentName { get; set; } = "";
        public decimal AmountDisbursed { get; set; }
        public DateTime? DisbursementDate { get; set; }
        public decimal TotalArrears { get; set; }
        public int DaysInArrears { get; set; }
        public decimal LoanBalance { get; set; }
        public int PaymentCount { get; set; }
        public decimal TotalPaid { get; set; }
    }

    public class PaymentFraudSignal
    {
        public int UnpostedCount { get; set; }
        public decimal UnpostedAmount { get; set; }
        public DateTime? OldestUnposted { get; set; }
        public DateTime? NewestUnposted { get; set; }
    }

    public class FraudAnalyticsReport
    {
        public FraudSummary Summary { get; set; } = new();
        public List<AgentFraudSignal> AgentSignals { get; set; } = new();
        public List<ShopFraudSignal> ShopSignals { get; set; } = new();
        public List<DuplicateIdentitySignal> DuplicateIdentities { get; set; } = new();
        public List<LoanStackingSignal> LoanStacking { get; set; } = new();
        public List<SuspiciousLoanSignal> SuspiciousLoans { get; set; } = new();
        public PaymentFraudSignal PaymentFraud { get; set; } = new();
    }
}

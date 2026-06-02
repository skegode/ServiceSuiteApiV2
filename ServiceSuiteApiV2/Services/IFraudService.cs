using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2.Services
{
    public interface IFraudService
    {
        Task<FraudAnalyticsReport> GetFraudReportAsync(string entityId);
        Task<List<AgentFraudSignal>> GetAgentFraudSignalsAsync(string entityId);
        Task<List<ShopFraudSignal>> GetShopFraudSignalsAsync(string entityId);
        Task<List<DuplicateIdentitySignal>> GetDuplicateIdentitiesAsync(string entityId);
        Task<List<LoanStackingSignal>> GetLoanStackingAsync(string entityId);
        Task<List<SuspiciousLoanSignal>> GetSuspiciousLoansAsync(string entityId, int minDaysInArrears = 30);
        Task<PaymentFraudSignal> GetPaymentFraudAsync(string entityId);
    }
}

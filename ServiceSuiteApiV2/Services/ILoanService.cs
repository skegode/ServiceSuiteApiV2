using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2.Controllers
{
    public interface ILoanService
    {
        /// <summary>
        /// Retrieves a single loan by its ID. 
        /// Must verify the loan belongs to the provided entityId.
        /// </summary>
        Task<LoanDto?> GetLoanByIdAsync(string entityId, string loanId);

        /// <summary>
        /// Retrieves filtered loans based on criteria (Min/Max days, amounts, etc.)
        /// </summary>
        Task<LoanResponse> GetLoansAsync(LoanFilterRequest filter);

        /// <summary>
        /// Retrieves a top list of active loans (Balance > 0) with optional search.
        /// </summary>
        Task<LoanResponse> GetActiveLoansAsync(string entityId, string? searchTerm, int top = 20);

        /// <summary>
        /// Retrieves detailed metadata/form items for a specific loan.
        /// </summary>
        Task<List<LoanDetailDto>> GetLoanDetailsAsync(string entityId, string loanId);

        /// <summary>
        /// Processes loan management actions (Waivers, Write-offs, etc.)
        /// </summary>
        Task<bool> ManageLoanAsync(LoanManagementRequest request);

        /// <summary>
        /// Processes a full loan write-off, clearing balances and updating statements.
        /// </summary>
        Task<bool> InitiateWriteoffAsync(WriteoffRequest request);

        /// </summary>
        Task<LoanBalanceDto?> GetLoanBalanceAsync(string entityId, string loanId);

        /// <summary>
        /// Finds a borrower by phone number, national ID, or borrower ID.
        /// </summary>
        Task<BorrowerDto?> GetBorrowerAsync(string entityId, string search);

        /// <summary>
        /// Returns loans disbursed within the given date range.
        /// </summary>
        Task<List<DisbursedLoanDto>> GetDisbursedLoansAsync(string entityId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns payments received within the given date range (from Transactions DB).
        /// </summary>
        Task<List<PaymentDto>> GetPaymentsAsync(string entityId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns loans that are past their due date, with optional minimum days in arrears filter.
        /// </summary>
        Task<LoanResponse> GetOverdueLoansAsync(string entityId, int minDays, int top);

        /// <summary>
        /// Returns all loans for a specific borrower matched by phone, national ID, or borrower ID.
        /// </summary>
        Task<LoanResponse> GetBorrowerLoansAsync(string entityId, string search);

        /// <summary>
        /// Returns the statement lines from customerstatement for a borrower matched by phone, national ID, or borrower ID.
        /// </summary>
        Task<BorrowerStatementDto?> GetBorrowerStatementAsync(string entityId, string search);

        /// <summary>
        /// Returns loans that have an installment due today (ExpectedDueDate = today, unpaid).
        /// </summary>
        Task<List<DueTodayLoanDto>> GetDueTodayLoansAsync(string entityId, int top = 500);

        /// <summary>
        /// Returns client details and their active loans matched by phone number, national ID, or borrower ID.
        /// </summary>
        Task<ClientProfileDto?> GetClientProfileAsync(string entityId, string search);
    }
}
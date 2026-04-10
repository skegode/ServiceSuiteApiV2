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
    }
}
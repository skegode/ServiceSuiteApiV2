using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceSuiteApiV2.Models;
using ServiceSuiteApiV2.Services;

[ApiController]
[Route("analytics/fraud")]
[Authorize]
public class FraudController : ControllerBase
{
    private readonly IFraudService _fraudService;

    private string UserEntityId => User.FindFirst("EntityId")?.Value
        ?? throw new UnauthorizedAccessException("EntityId missing in token.");

    public FraudController(IFraudService fraudService) => _fraudService = fraudService;

    /// <summary>
    /// Full fraud analytics report — all signals in a single call.
    /// Runs all sub-queries in parallel. Use for dashboard load.
    /// </summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetFraudReport()
    {
        var result = await _fraudService.GetFraudReportAsync(UserEntityId);
        return Ok(new ApiResponse<FraudAnalyticsReport>
        {
            Success = true,
            Message = BuildReportMessage(result.Summary),
            Data    = result
        });
    }

    /// <summary>
    /// Agent / onboarder fraud signals.
    /// Flags agents with high default rates, write-off concentration, or arrears-heavy portfolios.
    /// </summary>
    [HttpGet("agents")]
    public async Task<IActionResult> GetAgentSignals()
    {
        var result = await _fraudService.GetAgentFraudSignalsAsync(UserEntityId);
        int high = result.Count(a => a.RiskLevel == "High");
        int med  = result.Count(a => a.RiskLevel == "Medium");
        return Ok(new ApiResponse<List<AgentFraudSignal>>
        {
            Success = true,
            Message = $"{result.Count} agent(s) analysed — {high} High risk, {med} Medium risk.",
            Data    = result
        });
    }

    /// <summary>
    /// Shop / merchant fraud signals.
    /// Groups borrowers by their collection agent (shop operator) and flags shops
    /// where the majority of onboarded borrowers are defaulting.
    /// </summary>
    [HttpGet("shops")]
    public async Task<IActionResult> GetShopSignals()
    {
        var result = await _fraudService.GetShopFraudSignalsAsync(UserEntityId);
        int high = result.Count(s => s.RiskLevel == "High");
        return Ok(new ApiResponse<List<ShopFraudSignal>>
        {
            Success = true,
            Message = $"{result.Count} shop(s) analysed — {high} High risk.",
            Data    = result
        });
    }

    /// <summary>
    /// Borrowers sharing the same National ID — synthetic / stolen identity fraud.
    /// </summary>
    [HttpGet("borrowers/duplicate-ids")]
    public async Task<IActionResult> GetDuplicateIdentities()
    {
        var result = await _fraudService.GetDuplicateIdentitiesAsync(UserEntityId);
        return Ok(new ApiResponse<List<DuplicateIdentitySignal>>
        {
            Success = true,
            Message = $"{result.Count} duplicate National ID(s) detected.",
            Data    = result
        });
    }

    /// <summary>
    /// Borrowers holding more than one active loan simultaneously — loan stacking.
    /// </summary>
    [HttpGet("borrowers/stacking")]
    public async Task<IActionResult> GetLoanStacking()
    {
        var result = await _fraudService.GetLoanStackingAsync(UserEntityId);
        return Ok(new ApiResponse<List<LoanStackingSignal>>
        {
            Success = true,
            Message = $"{result.Count} borrower(s) with multiple active loans.",
            Data    = result
        });
    }

    /// <summary>
    /// Loans overdue with little or no repayment activity.
    /// Zero-payment loans surface first — strongest indicator of intentional fraud.
    /// </summary>
    [HttpGet("loans/suspicious")]
    public async Task<IActionResult> GetSuspiciousLoans([FromQuery] int minDays = 30)
    {
        if (minDays < 1)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "minDays must be at least 1." });

        var result = await _fraudService.GetSuspiciousLoansAsync(UserEntityId, minDays);
        int zeroPayment = result.Count(l => l.PaymentCount == 0);
        return Ok(new ApiResponse<List<SuspiciousLoanSignal>>
        {
            Success = true,
            Message = $"{result.Count} suspicious loan(s) — {zeroPayment} with zero repayments.",
            Data    = result
        });
    }

    /// <summary>
    /// Payments received but not posted after 3+ days — potential staff diversion of funds.
    /// </summary>
    [HttpGet("payments/unposted")]
    public async Task<IActionResult> GetPaymentFraud()
    {
        var result = await _fraudService.GetPaymentFraudAsync(UserEntityId);
        return Ok(new ApiResponse<PaymentFraudSignal>
        {
            Success = true,
            Message = result.UnpostedCount > 0
                ? $"Alert: {result.UnpostedCount} unposted payment(s) totalling {result.UnpostedAmount:N2} older than 3 days."
                : "No unposted payment anomalies detected.",
            Data = result
        });
    }

    private static string BuildReportMessage(FraudSummary s) =>
        $"Fraud report generated. High-risk agents: {s.HighRiskAgentCount}, " +
        $"High-risk shops: {s.HighRiskShopCount}, " +
        $"Duplicate IDs: {s.DuplicateIdentityCount}, " +
        $"Loan stacking: {s.LoanStackingCount}, " +
        $"Zero-payment loans: {s.ZeroPaymentLoanCount}. " +
        $"Estimated exposure: {s.EstimatedFraudExposure:N2}.";
}

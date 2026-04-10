using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceSuiteApiV2;
using ServiceSuiteApiV2.Controllers;
using ServiceSuiteApiV2.Models;

[ApiController]
[Route("loans")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    // Helper to get EntityId from the Token claims
    private string UserEntityId => User.FindFirst("EntityId")?.Value
        ?? throw new UnauthorizedAccessException("EntityId missing in token.");

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpPost("GetLoansByFilter")]   
    public async Task<IActionResult> GetLoans([FromBody] LoanFilterRequest filter)
    {
        filter.EntityId = UserEntityId;  

        var result = await _loanService.GetLoansAsync(filter);
        return Ok(new ApiResponse<LoanResponse> { Success = true, Data = result });
    }

    [HttpGet("ActiveLoans")]
    public async Task<IActionResult> GetActiveLoans([FromQuery] string? search, [FromQuery] int top = 20)
    {
        // Pass the EntityId from token to the service
        var result = await _loanService.GetActiveLoansAsync(UserEntityId, search, top);
        return Ok(new ApiResponse<LoanResponse> { Success = true, Data = result });
    }

    [HttpGet("details/{loanId}")]
    public async Task<IActionResult> GetLoanDetails(string loanId)
    {
        // We pass UserEntityId to ensure the user can't peek at other entities' loans
        var result = await _loanService.GetLoanDetailsAsync(UserEntityId, loanId);
        return Ok(new ApiResponse<List<LoanDetailDto>> { Success = true, Data = result });
    }
    [HttpGet("{loanId}")]
    [HttpGet("GetLoanById/{loanId}")]

    public async Task<IActionResult> GetLoanById(string loanId)
    {
        // UserEntityId is extracted from the JWT as we set up earlier
        var result = await _loanService.GetLoanByIdAsync(UserEntityId, loanId);

        if (result == null)
        {
            return NotFound(new ApiResponse<LoanDto>
            {
                Success = false,
                Message = $"Loan {loanId} not found or access denied."
            });
        }

        return Ok(new ApiResponse<LoanDto>
        {
            Success = true,
            Message = "Loan retrieved successfully.",
            Data = result
        });
    }

    [HttpPost("manage")]
    public async Task<IActionResult> ManageLoan([FromBody] LoanManagementRequest request)
    {
        try
        {
            request.EntityId = UserEntityId; // Enforce the entity context
            var success = await _loanService.ManageLoanAsync(request);
            return Ok(new ApiResponse<bool> { Success = true, Data = success });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<bool> { Success = false, Message = ex.Message });
        }
    }
    [HttpPost("initiate-writeoff")]
    public async Task<IActionResult> InitiateWriteoff([FromBody] WriteoffRequest request)
    {
        // 1. Basic Validation
        if (string.IsNullOrEmpty(request.LoanId) || request.CurrentOlb <= 0)
        {
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Invalid Loan ID or Loan Balance must be greater than zero."
            });
        }

        try
        {
            // 2. Inject Security Context from the JWT Token
            // These properties should be available if you are using a BaseController 
            // or extracting claims manually.
            request.EntityId = UserEntityId;

            // 3. Call the Service
            var result = await _loanService.InitiateWriteoffAsync(request);

            if (result)
            {
                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Loan write-off processed successfully. Balance cleared."
                });
            }

            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "The write-off could not be completed at this time."
            });
        }
        catch (Exception ex)
        {
            // Log the error (e.g., to Serilog or Application Insights)
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = $"Internal Server Error: {ex.Message}"
            });
        }
    }
    [HttpGet("balance/{loanId}")]
    public async Task<IActionResult> GetLoanBalance(string loanId)
    {
        try
        {
            // Use the helper UserEntityId to restrict access
            var result = await _loanService.GetLoanBalanceAsync(UserEntityId, loanId);

            if (result == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Balance for loan {loanId} not found or access denied."
                });
            }

            return Ok(new ApiResponse<LoanBalanceDto>
            {
                Success = true,
                Message = "Balance retrieved successfully.",
                Data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = $"Error retrieving balance: {ex.Message}"
            });
        }
    }
}
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

    //[HttpPost("manage")]
    //public async Task<IActionResult> ManageLoan([FromBody] LoanManagementRequest request)
    //{
    //    try
    //    {
    //        request.EntityId = UserEntityId;
    //        var success = await _loanService.ManageLoanAsync(request);
    //        return Ok(new ApiResponse<bool> { Success = true, Data = success });
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest(new ApiResponse<bool> { Success = false, Message = ex.Message });
    //    }
    //}

    //[HttpPost("initiate-writeoff")]
    //public async Task<IActionResult> InitiateWriteoff([FromBody] WriteoffRequest request)
    //{
    //    if (string.IsNullOrEmpty(request.LoanId) || request.CurrentOlb <= 0)
    //    {
    //        return BadRequest(new ApiResponse<string>
    //        {
    //            Success = false,
    //            Message = "Invalid Loan ID or Loan Balance must be greater than zero."
    //        });
    //    }

    //    try
    //    {
    //        request.EntityId = UserEntityId;
    //        var result = await _loanService.InitiateWriteoffAsync(request);

    //        if (result)
    //        {
    //            return Ok(new ApiResponse<string>
    //            {
    //                Success = true,
    //                Message = "Loan write-off processed successfully. Balance cleared."
    //            });
    //        }

    //        return BadRequest(new ApiResponse<string>
    //        {
    //            Success = false,
    //            Message = "The write-off could not be completed at this time."
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        return StatusCode(500, new ApiResponse<string>
    //        {
    //            Success = false,
    //            Message = $"Internal Server Error: {ex.Message}"
    //        });
    //    }
    //}
    [HttpGet("disbursements")]
    public async Task<IActionResult> GetDisbursedLoans([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "startDate must be before endDate." });

        var result = await _loanService.GetDisbursedLoansAsync(UserEntityId, startDate, endDate);
        return Ok(new ApiResponse<List<DisbursedLoanDto>>
        {
            Success = true,
            Message = $"{result.Count} disbursement(s) found.",
            Data = result
        });
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "startDate must be before endDate." });

        var result = await _loanService.GetPaymentsAsync(UserEntityId, startDate, endDate);
        return Ok(new ApiResponse<List<PaymentDto>>
        {
            Success = true,
            Message = $"{result.Count} payment(s) found.",
            Data = result
        });
    }

    [HttpGet("borrower/loans")]
    public async Task<IActionResult> GetBorrowerLoans([FromQuery] string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return BadRequest(new ApiResponse<object> { Success = false, Message = "search parameter is required." });

        var result = await _loanService.GetBorrowerLoansAsync(UserEntityId, search.Trim());

        if (result.Count == 0)
            return NotFound(new ApiResponse<object> { Success = false, Message = "No loans found for the given borrower." });

        return Ok(new ApiResponse<LoanResponse>
        {
            Success = true,
            Message = $"{result.Count} loan(s) found.",
            Data = result
        });
    }

    [HttpGet("borrower/statement")]
    public async Task<IActionResult> GetBorrowerStatement([FromQuery] string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return BadRequest(new ApiResponse<BorrowerStatementDto> { Success = false, Message = "search parameter is required." });

        var result = await _loanService.GetBorrowerStatementAsync(UserEntityId, search.Trim());

        if (result == null)
            return NotFound(new ApiResponse<BorrowerStatementDto> { Success = false, Message = "Borrower not found." });

        return Ok(new ApiResponse<BorrowerStatementDto>
        {
            Success = true,
            Message = $"{result.TotalLines} statement line(s) found.",
            Data = result
        });
    }

    [HttpGet("borrower")]
    public async Task<IActionResult> GetBorrower([FromQuery] string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return BadRequest(new ApiResponse<BorrowerDto> { Success = false, Message = "search parameter is required." });

        var result = await _loanService.GetBorrowerAsync(UserEntityId, search.Trim());

        if (result == null)
            return NotFound(new ApiResponse<BorrowerDto> { Success = false, Message = "Borrower not found." });

        return Ok(new ApiResponse<BorrowerDto> { Success = true, Data = result });
    }

    [HttpGet("borrower/profile")]
    public async Task<IActionResult> GetClientProfile([FromQuery] string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return BadRequest(new ApiResponse<ClientProfileDto> { Success = false, Message = "search parameter is required." });

        var result = await _loanService.GetClientProfileAsync(UserEntityId, search.Trim());

        if (result == null)
            return NotFound(new ApiResponse<ClientProfileDto> { Success = false, Message = "Client not found." });

        return Ok(new ApiResponse<ClientProfileDto>
        {
            Success = true,
            Message = $"{result.ActiveLoans.Count} active loan(s) found.",
            Data = result
        });
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueLoans([FromQuery] int minDays = 1, [FromQuery] int top = 50)
    {
        if (minDays < 1)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "minDays must be at least 1." });

        var result = await _loanService.GetOverdueLoansAsync(UserEntityId, minDays, top);
        return Ok(new ApiResponse<LoanResponse>
        {
            Success = true,
            Message = $"{result.Count} overdue loan(s) found with at least {minDays} day(s) in arrears.",
            Data = result
        });
    }

    [HttpGet("due-today")]
    public async Task<IActionResult> GetDueTodayLoans([FromQuery] int top = 500)
    {
        var result = await _loanService.GetDueTodayLoansAsync(UserEntityId, top);
        return Ok(new ApiResponse<List<DueTodayLoanDto>>
        {
            Success = true,
            Message = $"{result.Count} loan(s) due today.",
            Data = result
        });
    }

    [HttpPost("borrower")]
    public async Task<IActionResult> AddNewBorrower([FromBody] NewBorrowerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return BadRequest(new ApiResponse<NewBorrowerResultDto>
            {
                Success = false,
                Message = "FirstName and PhoneNumber are required.",
                Data = new NewBorrowerResultDto { Success = false, ErrorCode = "MISSING_REQUIRED_FIELD", Message = "FirstName and PhoneNumber are required." }
            });
        }

        request.EntityId = UserEntityId;

        try
        {
            var result = await _loanService.AddNewBorrowerAsync(request);

            if (!result.Success)
                return BadRequest(new ApiResponse<NewBorrowerResultDto> { Success = false, Message = result.Message, Data = result });

            return Ok(new ApiResponse<NewBorrowerResultDto> { Success = true, Message = result.Message, Data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string> { Success = false, Message = $"Error creating borrower: {ex.Message}" });
        }
    }

    [HttpPost("history")]
    public async Task<IActionResult> AddHistoryLoan([FromBody] HistoryLoanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BorrowerId) || string.IsNullOrWhiteSpace(request.ProductId) || request.Principal <= 0)
        {
            const string msg = "BorrowerId, ProductId, and a positive Principal are required.";
            return BadRequest(new ApiResponse<HistoryLoanResultDto>
            {
                Success = false,
                Message = msg,
                Data = new HistoryLoanResultDto { Success = false, ErrorCode = "MISSING_REQUIRED_FIELD", Response = msg }
            });
        }

        if (string.IsNullOrWhiteSpace(request.TransactionRef))
        {
            const string msg = "TransactionRef is required for a history loan.";
            return BadRequest(new ApiResponse<HistoryLoanResultDto>
            {
                Success = false,
                Message = msg,
                Data = new HistoryLoanResultDto { Success = false, ErrorCode = "MISSING_TRANSACTION_REF", Response = msg }
            });
        }

        if (request.BorrowDate == default || request.BorrowDate > DateTime.UtcNow)
        {
            const string msg = "BorrowDate is required and cannot be in the future.";
            return BadRequest(new ApiResponse<HistoryLoanResultDto>
            {
                Success = false,
                Message = msg,
                Data = new HistoryLoanResultDto { Success = false, ErrorCode = "INVALID_BORROW_DATE", Response = msg }
            });
        }

        request.EntityId = UserEntityId;

        try
        {
            var result = await _loanService.AddHistoryLoanAsync(request);

            if (!result.Success)
                return BadRequest(new ApiResponse<HistoryLoanResultDto> { Success = false, Message = result.Response, Data = result });

            return Ok(new ApiResponse<HistoryLoanResultDto> { Success = true, Message = result.Response, Data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<string> { Success = false, Message = $"Error creating history loan: {ex.Message}" });
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
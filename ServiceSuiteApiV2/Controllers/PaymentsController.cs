using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceSuiteApiV2.Models;

[ApiController]
[Route("payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ServiceSuiteApiV2.IStkService _stkService;

    private int UserEntityId
    {
        get
        {
            var val = User.FindFirst("EntityId")?.Value
                ?? throw new UnauthorizedAccessException("EntityId missing in token.");
            return int.TryParse(val, out int id)
                ? id
                : throw new UnauthorizedAccessException("EntityId in token is not a valid integer.");
        }
    }

    public PaymentsController(ServiceSuiteApiV2.IStkService stkService)
    {
        _stkService = stkService;
    }

    [HttpPost("stk-push")]
    public async Task<IActionResult> InitiateStkPush([FromBody] StkPushRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Amount must be greater than zero."
            });

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "PhoneNumber is required."
            });

        var result = await _stkService.InitiateStkPushAsync(
            request.Amount,
            request.PhoneNumber,
            UserEntityId,
            request.AccountReference,
            request.TransactionDesc ?? "Payment");

        if (string.IsNullOrEmpty(result))
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Message = "STK push request failed. Check server logs for details."
            });

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "STK push initiated.",
            Data = result
        });
    }
}

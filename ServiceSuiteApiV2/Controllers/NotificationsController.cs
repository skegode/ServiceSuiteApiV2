using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceSuiteApiV2.Models;

[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ServiceSuiteApiV2.ISmsService _smsService;

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

    public NotificationsController(ServiceSuiteApiV2.ISmsService smsService)
    {
        _smsService = smsService;
    }

    [HttpPost("sms")]
    public async Task<IActionResult> SendSms([FromBody] SmsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "Message is required."
            });

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = "PhoneNumber is required."
            });

        var (success, msg) = await _smsService.SendSmsAsync(
            request.Message,
            request.PhoneNumber,
            UserEntityId,
            request.ScheduleDate);

        if (!success)
            return BadRequest(new ApiResponse<string> { Success = false, Message = msg });

        return Ok(new ApiResponse<string> { Success = true, Message = msg });
    }
}

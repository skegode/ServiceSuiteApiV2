using Microsoft.AspNetCore.Mvc;
using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

      
        [HttpPost("token")]
        public async Task<IActionResult> GetToken([FromBody] TokenRequest request)
        {
            // 1. Basic Validation
            if (string.IsNullOrWhiteSpace(request.ClientId) ||
                string.IsNullOrWhiteSpace(request.ClientSecret) ||
                string.IsNullOrWhiteSpace(request.EntityId.ToString()   )) 
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "client_id, client_secret, and EntityId are required."
                });
            }

            bool isValid = await _authService.ValidateClientAsync(request.ClientId, request.ClientSecret,request.EntityId.ToString());

            if (!isValid)
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Invalid client credentials."
                });
            }

         
            var token = await _authService.GenerateTokenAsync(request.ClientId, request.EntityId.ToString());

            return Ok(new ApiResponse<TokenResponse>
            {
                Success = true,
                Message = "Token generated successfully.",
                Data = token
            });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ServiceSuiteApiV2.Models;
using ServiceSuiteApiV2;

namespace ServiceSuiteApiV2.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

      
        [HttpPost("token")]
        [EnableRateLimiting("token-endpoint")]
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

        [HttpPost("clients")]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
        {
            var adminKey = _config["AdminKey"];
            if (!Request.Headers.TryGetValue("X-Admin-Key", out var providedKey) || providedKey != adminKey)
                return Unauthorized(new ApiResponse<string> { Success = false, Message = "Invalid or missing X-Admin-Key." });

            if (string.IsNullOrWhiteSpace(request.EntityId) || string.IsNullOrWhiteSpace(request.ClientName))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "EntityId and ClientName are required." });

            var result = await _authService.CreateClientAsync(request.EntityId, request.ClientName);

            return Ok(new ApiResponse<CreateClientResponse>
            {
                Success = true,
                Message = "Client saved. Store the ClientSecret now — it will not be shown again.",
                Data = result
            });
        }

    }
}

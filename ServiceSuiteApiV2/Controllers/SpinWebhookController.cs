using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceSuiteApiV2.Models;
using ServiceSuiteApiV2.Services;

namespace ServiceSuiteApiV2.Controllers
{
    [ApiController]
    [Route("webhooks")]
    public class SpinWebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SpinWebhookController> _logger;
        private readonly ISpinWebhookPersistenceService _persistenceService;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public SpinWebhookController(IConfiguration config, ILogger<SpinWebhookController> logger, ISpinWebhookPersistenceService persistenceService)
        {
            _config = config;
            _logger = logger;
            _persistenceService = persistenceService;
        }

        [HttpPost("spin-analysis")]
        public async Task<IActionResult> Receive()
        {
            if (!ValidateBasicAuth(out var authError))
                return Unauthorized(new { success = false, message = authError });

            var rawBody = await new System.IO.StreamReader(Request.Body).ReadToEndAsync();

            SpinWebhookPayload payload;
            try { payload = JsonSerializer.Deserialize<SpinWebhookPayload>(rawBody) ?? new SpinWebhookPayload(); }
            catch { payload = new SpinWebhookPayload(); }

            var logEntry = new
            {
                ReceivedAt = DateTime.UtcNow,
                RawBody = rawBody,
                ParsedPayload = payload
            };

            var json = JsonSerializer.Serialize(logEntry, _jsonOptions);

            try
            {
                await AppendToLogFileAsync(json);
                _logger.LogInformation("[SpinWebhook] Payload logged. file_unique_id={FileUniqueId} file_type={FileType} state={State}",
                    payload.FileUniqueId, payload.FileType, payload.StateName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpinWebhook] Failed to write log file. RawBody={RawBody}", rawBody);
            }

            try
            {
                await _persistenceService.SavePayloadAsync(payload, rawBody, logEntry.ReceivedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpinWebhook] Failed to save payload to database. file_unique_id={FileUniqueId}", payload.FileUniqueId);
            }

            return Ok(new { success = true, message = "Webhook received." });
        }

        private bool ValidateBasicAuth(out string error)
        {
            error = string.Empty;

            if (!Request.Headers.TryGetValue("Authorization", out var authHeader) ||
                !authHeader.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                error = "Missing or invalid Authorization header.";
                return false;
            }

            string encoded = authHeader.ToString()["Basic ".Length..].Trim();
            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            catch
            {
                error = "Malformed Basic Auth credentials.";
                return false;
            }

            var parts = decoded.Split(':', 2);
            if (parts.Length != 2)
            {
                error = "Malformed Basic Auth credentials.";
                return false;
            }

            var expectedUser = _config["SpinWebhook:Username"] ?? "";
            var expectedPass = _config["SpinWebhook:Password"] ?? "";

            if (parts[0] != expectedUser || parts[1] != expectedPass)
            {
                error = "Invalid webhook credentials.";
                return false;
            }

            return true;
        }

        private async Task AppendToLogFileAsync(string json)
        {
            var basePath = _config["SpinWebhook:LogPath"] ?? "Logs/spin_webhook";
            var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var filePath = $"{basePath}_{date}.log";

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            var separator = new string('-', 80);
            var entry = $"{separator}{Environment.NewLine}{json}{Environment.NewLine}";

            await System.IO.File.AppendAllTextAsync(filePath, entry);
        }
    }
}

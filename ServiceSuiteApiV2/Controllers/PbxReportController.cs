using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ServiceSuiteApiV2.Helpers;
using ServiceSuiteApiV2.Models.PbxReports;

namespace ServiceSuiteApiV2.Controllers
{
    // Receives Yeastar S-Series PBX "API Reports" (its push/webhook mechanism).
    // Route mirrors the path already registered with the PBX (application/service/Report) so
    // only the host/port need to change when this replaces the old PbxWebApi receiver.
    [Route("application/service")]
    [ApiController]
    public class PbxReportController : ControllerBase
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IConfiguration _config;
        private string ConnectionString => _config.GetConnectionString("CallBoxConnection")!;

        public PbxReportController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("Report")]
        public async Task<IActionResult> Reports()
        {
            string rawJson;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawJson = await reader.ReadToEndAsync();
            }

            await PbxReportLogger.WriteLogAsync(rawJson);

            var discriminator = JsonSerializer.Deserialize<ActionResponse>(rawJson, _jsonOptions);
            var action = discriminator?.action ?? "";

            await using var conn = new SqlConnection(ConnectionString);

            string result;
            switch (action)
            {
                case "ALERT":
                    result = await HandleAlert(rawJson, conn);
                    break;

                case "RING":
                    result = await HandleRing(rawJson, conn, action);
                    break;

                case "BYE":
                    result = await HandleRing(rawJson, conn, action);
                    break;

                case "NewCdr":
                    result = await HandleCdr(rawJson, conn);
                    break;

                case "ANSWER":
                case "ANSWERED":
                    result = await HandleAnswer(rawJson, conn, action);
                    break;

                case "Tranfer": // sic — matches Yeastar's own field value
                    result = await HandleTransfer(rawJson, conn);
                    break;

                case "CallFailed":
                    result = await HandleCallFailure(rawJson, conn);
                    break;

                case "DTMF":
                case "DTMF event":
                    result = await HandleKeypress(rawJson, conn);
                    break;

                case "ExtensionStatus":
                    result = await HandleExtensionStatus(rawJson, conn);
                    break;

                case "BootUp":
                    result = await HandleBootUp(rawJson, conn);
                    break;

                case "Invite":
                case "Incoming":
                    result = await HandleInboundCallEvent(rawJson, conn, action);
                    break;

                default:
                    result = $"Unhandled action '{action}' — logged only.";
                    break;
            }

            return Ok(result);
        }

        private static ExtDetails? FindExt(List<CallDetail>? call, int skip = 0) =>
            call?.Where(c => c.Ext != null).Select(c => c.Ext).Skip(skip).FirstOrDefault();

        private static OutboundCall? FindOutbound(List<CallDetail>? call) =>
            call?.Select(c => c.Outbound).FirstOrDefault(o => o != null);

        private static InboundCall? FindInbound(List<CallDetail>? call) =>
            call?.Select(c => c.Inbound).FirstOrDefault(i => i != null);

        private static object? ParseExtId(string? extid) =>
            int.TryParse(extid, out var id) ? id : null;

        private async Task<string> HandleAlert(string rawJson, SqlConnection conn)
        {
            var report = JsonSerializer.Deserialize<CallReport>(rawJson, _jsonOptions);
            var ext = FindExt(report?.Call);

            await conn.ExecuteAsync("sp_insertcallalert", new
            {
                callid = report?.CallId,
                sn = report?.Sn,
                extno = ParseExtId(ext?.ExtId),
                action = report?.Action
            }, commandType: CommandType.StoredProcedure);

            return "ALERT logged to CallAlerts.";
        }

        private async Task<string> HandleRing(string rawJson, SqlConnection conn, string action)
        {
            var report = JsonSerializer.Deserialize<CallReport>(rawJson, _jsonOptions);
            var outbound = FindOutbound(report?.Call);
            var inbound = FindInbound(report?.Call);
            var ext = FindExt(report?.Call);

            await conn.ExecuteAsync("sp_insertcallrings", new
            {
                callid = report?.CallId,
                sn = report?.Sn,
                extno = ParseExtId(ext?.ExtId),
                callfrom = outbound?.From ?? inbound?.From,
                callto = outbound?.To ?? inbound?.To,
                trunk = outbound?.Trunk ?? inbound?.Trunk,
                outboundid = outbound?.OutboundId ?? inbound?.InboundId,
                action
            }, commandType: CommandType.StoredProcedure);

            return $"{action} logged to CallRings.";
        }

        private async Task<string> HandleCdr(string rawJson, SqlConnection conn)
        {
            var c = JsonSerializer.Deserialize<CdrReport>(rawJson, _jsonOptions);
            object timestart = DateTime.TryParse(c?.timestart, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var ts) ? ts : DBNull.Value;

            await conn.ExecuteAsync("sp_insertcallcdr", new
            {
                callid = c?.callid,
                sn = c?.sn,
                timestart,
                callfrom = c?.callfrom,
                callto = c?.callto,
                type = c?.type,
                recording = c?.recording,
                didnumber = c?.didnumber,
                agentringtime = c?.agentringtime,
                callduraction = c?.callduraction,
                talkduraction = c?.talkduraction,
                srctrunkname = c?.srctrunkname,
                dsttrcunkname = c?.dsttrcunkname,
                pincode = c?.pincode,
                status = c?.status
            }, commandType: CommandType.StoredProcedure);

            return "NewCdr logged to callcdr.";
        }

        private async Task<string> HandleAnswer(string rawJson, SqlConnection conn, string action)
        {
            var report = JsonSerializer.Deserialize<CallReport>(rawJson, _jsonOptions);
            var ext = FindExt(report?.Call);
            var outbound = FindOutbound(report?.Call);
            var inbound = FindInbound(report?.Call);

            await conn.ExecuteAsync("sp_InsertCallAnswer", new
            {
                callid = report?.CallId,
                sn = report?.Sn,
                extid = ParseExtId(ext?.ExtId),
                callfrom = outbound?.From ?? inbound?.From,
                callto = outbound?.To ?? inbound?.To,
                trunk = outbound?.Trunk ?? inbound?.Trunk,
                inboundid = inbound?.InboundId,
                outboundid = outbound?.OutboundId,
                action
            }, commandType: CommandType.StoredProcedure);

            return $"{action} logged to CallAnswers.";
        }

        private async Task<string> HandleTransfer(string rawJson, SqlConnection conn)
        {
            var report = JsonSerializer.Deserialize<CallReport>(rawJson, _jsonOptions);
            var fromExt = FindExt(report?.Call, skip: 0);
            var toExt = FindExt(report?.Call, skip: 1);
            var inbound = FindInbound(report?.Call);

            await conn.ExecuteAsync("sp_InsertCallTransfer", new
            {
                callid = report?.CallId,
                sn = report?.Sn,
                fromextid = ParseExtId(fromExt?.ExtId),
                toextid = ParseExtId(toExt?.ExtId),
                inboundfrom = inbound?.From,
                inboundto = inbound?.To,
                inboundtrunk = inbound?.Trunk,
                inboundid = inbound?.InboundId
            }, commandType: CommandType.StoredProcedure);

            return "Tranfer logged to CallTransfers.";
        }

        private async Task<string> HandleCallFailure(string rawJson, SqlConnection conn)
        {
            var report = JsonSerializer.Deserialize<CallFailureReport>(rawJson, _jsonOptions);

            await conn.ExecuteAsync("sp_InsertCallFailure", new
            {
                callid = report?.CallId,
                sn = report?.Sn,
                reason = report?.Reason,
                extid = ParseExtId(report?.Ext?.ExtId),
                callfrom = report?.Outbound?.From,
                callto = report?.Outbound?.To,
                trunk = report?.Outbound?.Trunk,
                outboundid = report?.Outbound?.OutboundId
            }, commandType: CommandType.StoredProcedure);

            return "CallFailed logged to CallFailures.";
        }

        private async Task<string> HandleKeypress(string rawJson, SqlConnection conn)
        {
            var report = JsonSerializer.Deserialize<CallReport>(rawJson, _jsonOptions);
            var ext = FindExt(report?.Call);

            await conn.ExecuteAsync("sp_InsertCallKeypress", new
            {
                callid = report?.CallId,
                sn = report?.Sn,
                extid = ParseExtId(ext?.ExtId),
                info = report?.Info,
                infos = report?.Infos,
                flag = int.TryParse(report?.Flag, out var f) ? f : (int?)null
            }, commandType: CommandType.StoredProcedure);

            return "DTMF logged to CallKeypresses.";
        }

        private async Task<string> HandleExtensionStatus(string rawJson, SqlConnection conn)
        {
            var report = JsonSerializer.Deserialize<ExtensionStatusReport>(rawJson, _jsonOptions);

            await conn.ExecuteAsync("sp_LogExtensionStatus", new
            {
                extension = report?.Extension,
                status = report?.Status,
                registeredIP = report?.RegisteredIP,
                sn = report?.Sn
            }, commandType: CommandType.StoredProcedure);

            return "ExtensionStatus logged to ExtensionStatusLog.";
        }

        private async Task<string> HandleBootUp(string rawJson, SqlConnection conn)
        {
            var discriminator = JsonSerializer.Deserialize<Dictionary<string, string>>(rawJson, _jsonOptions);
            var sn = discriminator != null && discriminator.TryGetValue("sn", out var s) ? s : null;

            await conn.ExecuteAsync("sp_InsertSystemStartup", new { sn },
                commandType: CommandType.StoredProcedure);

            return "BootUp logged to SystemStartupLog.";
        }

        private async Task<string> HandleInboundCallEvent(string rawJson, SqlConnection conn, string action)
        {
            var report = JsonSerializer.Deserialize<CallReport>(rawJson, _jsonOptions);
            var inbound = FindInbound(report?.Call);

            await conn.ExecuteAsync("sp_InsertInboundCallEvent", new
            {
                action,
                callid = report?.CallId,
                sn = report?.Sn,
                callfrom = inbound?.From,
                callto = inbound?.To,
                trunk = inbound?.Trunk,
                inboundid = inbound?.InboundId
            }, commandType: CommandType.StoredProcedure);

            return $"{action} logged to InboundCallEvents.";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MDMIntegration;
using TrustonicLockingApp.Models;


namespace TrustonicLockingApp
{
    internal class IphoneLocker
    {
        private readonly ChiniseMdm _chineseService = new ChiniseMdm();
        private readonly MarenaMdmService _marenaService = new MarenaMdmService();
        private readonly string _constr = System.Configuration.ConfigurationManager.ConnectionStrings["constr"].ConnectionString;
        private readonly Logs _logs = new Logs();
        private readonly ZohoCliqNotifier _cliq = new ZohoCliqNotifier();

        private const int EntityId8 = 8;
        private const string Channel = "keunlockingrequests";

        // Accumulates across multiple worker cycles (each cycle handles at most 50) so the
        // "completed" summary is only sent once the whole queue is drained, not per-batch.
        private int _cumulativeLockedCount = 0;

        public async Task ProcessLockPhoneAsync()
        {
            try
            {
                await using var conn = new SqlConnection(_constr);
                await conn.OpenAsync().ConfigureAwait(false);

                // Devices already recorded in UnknownDevices are excluded from the SELECT below
                // (nothing left to retry there) but every re-queued row for them would otherwise
                // sit at IsProcessed = 0 forever, since the SELECT can never pick them up again to
                // reach the "mark processed" step. Drain those here instead.
                const string clearKnownUnknownQuery = @"
            UPDATE p
            SET p.IsProcessed = 1
            FROM PhoneLockRequests p
            WHERE p.Locktypeid = 1
              AND p.IsProcessed = 0
              AND EXISTS (SELECT 1 FROM UnknownDevices u WHERE u.SerialNo = p.SerialNo)";
                await using (var clearCmd = new SqlCommand(clearKnownUnknownQuery, conn))
                {
                    await clearCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                const string selectQuery = @"
            SELECT TOP 50 p.SerialNo, p.EntityId,
                   CASE WHEN ld.stringValue LIKE '%IPHONE%' THEN 1 ELSE 0 END AS IsIPhone,
                   p.LoanId
            FROM PhoneLockRequests p
            LEFT JOIN LoanDetails ld ON ld.loanId = p.LoanId AND ld.itemId = 13
            WHERE p.Locktypeid = 1
              AND p.IsProcessed = 0
              AND NOT EXISTS (SELECT 1 FROM UnknownDevices u WHERE u.SerialNo = p.SerialNo)";

                var phonesToProcess = new List<(string SerialNo, int EntityId, bool IsIPhone, int? LoanId)>();

                // Fetch phones and handle nulls
                await using (var cmd = new SqlCommand(selectQuery, conn))
                await using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        string serialNo = reader.IsDBNull(0) ? null : reader.GetString(0);
                        int? entityId = reader.IsDBNull(1) ? null : reader.GetInt32(1);

                        if (serialNo == null || entityId == null)
                        {
                            await _logs.LogsAsync($"Skipped row due to NULL: SerialNo={(serialNo ?? "NULL")}, EntityId={(entityId?.ToString() ?? "NULL")}", 5);
                            continue;
                        }

                        phonesToProcess.Add((serialNo, entityId.Value, reader.GetInt32(2) == 1, reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)));
                    }
                }

                if (phonesToProcess.Count == 0)
                {
                    Console.WriteLine("No unpaid clients found to lock.");
                    return;
                }

                Console.WriteLine($"Fetched {phonesToProcess.Count} devices to process.");

                foreach (var phone in phonesToProcess)
                {
                    string imei = phone.SerialNo.Trim();
                    int entityId = phone.EntityId;
                    bool isIPhone = phone.IsIPhone;
                    int? loanId = phone.LoanId;

                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        await _logs.LogsAsync($"Skipped empty SerialNo for EntityId: {entityId}", 5);
                        continue;
                    }

                    Console.WriteLine($"Processing IMEI: {imei}");

                    try
                    {
                        bool locked = false;
                        bool rateLimited = false;
                        string winningProvider = null;
                        int chineseClearCode = -1, androidCode = -1, marenaClearCode = -1;
                        string chineseClearMsg = "", androidMsg = "", marenaClearMsg = "";

                        // Fast path: if we already know which MDM works for this device, try it directly first.
                        string knownProvider = await GetKnownProviderAsync(conn, imei).ConfigureAwait(false);
                        if (knownProvider == "MarenaMDM")
                        {
                            (marenaClearCode, marenaClearMsg) = await _marenaService.ClearLockAsync(imei, entityId).ConfigureAwait(false);
                            if (marenaClearCode == 200)
                            {
                                var r = await _marenaService.LockPhoneAsync(imei, entityId).ConfigureAwait(false);
                                if (r.Code == 200) { locked = true; winningProvider = "MarenaMDM"; }
                            }
                        }
                        else if (knownProvider == "ChineseMDM-Android")
                        {
                            (androidCode, androidMsg) = await _chineseService.LockAndroidPhoneAsync(imei, entityId).ConfigureAwait(false);
                            if (androidCode == 200 || androidCode == -1409) { locked = true; winningProvider = "ChineseMDM-Android"; }
                        }
                        else if (knownProvider == "ChineseMDM")
                        {
                            (chineseClearCode, chineseClearMsg) = await _chineseService.ClearLockAsync(imei, entityId).ConfigureAwait(false);
                            if (chineseClearCode == 200)
                            {
                                var r = await _chineseService.LockPhoneAsync(imei, entityId).ConfigureAwait(false);
                                if (r.Code == 200 || r.Code == -1409) { locked = true; winningProvider = "ChineseMDM"; }
                            }
                        }

                        if (locked)
                        {
                            await _logs.LogsAsync($"Device {imei} locked via cached provider {knownProvider}.", 5);
                        }
                        else
                        {
                        // Cache miss, or the cached provider no longer works: fall through to the full cascade.
                        if (chineseClearCode == -1)
                            (chineseClearCode, chineseClearMsg) = await _chineseService.ClearLockAsync(imei, entityId).ConfigureAwait(false);

                        if (chineseClearCode == -1400)
                        {
                            // "-1400: too many commands awaiting response" means this device
                            // already has a lock command queued/in flight at Chinese MDM — not
                            // that the device is unknown. Don't fall back to Android/Marena (that
                            // would misclassify a live device) and don't send a "Failed" message.
                            // Mark it processed so we don't pile more redundant commands on top of
                            // the one already queued.
                            rateLimited = true;
                            await _logs.LogsAsync($"Device {imei} already has a pending command queued at Chinese MDM (-1400 on clear-lock); leaving as-is.", 5);
                            await _cliq.SendMessageAsync(EntityId8, Channel,
                                $"⏳ *Device Lock Already Queued*\n• *SerialNo:* {imei}\n• *Reason:* Chinese MDM already has a lock command pending for this device.").ConfigureAwait(false);
                        }
                        else if (chineseClearCode == 200)
                        {
                            var lockResponse = await _chineseService.LockPhoneAsync(imei, entityId).ConfigureAwait(false);
                            Console.WriteLine($"Device {imei} locked via Chinese MDM → {lockResponse.Code} {lockResponse.Message}");

                            if (lockResponse.Code == 200 || lockResponse.Code == -1409)
                            {
                                await _logs.LogsAsync($"Device {imei} locked via Chinese MDM successfully.", 5);
                                locked = true;
                                winningProvider = "ChineseMDM";
                            }
                            else if (lockResponse.Code == -1400)
                            {
                                rateLimited = true;
                                await _logs.LogsAsync($"Device {imei} already has a pending command queued at Chinese MDM (-1400 on lock); leaving as-is.", 5);
                                await _cliq.SendMessageAsync(EntityId8, Channel,
                                    $"⏳ *Device Lock Already Queued*\n• *SerialNo:* {imei}\n• *Reason:* Chinese MDM already has a lock command pending for this device.").ConfigureAwait(false);
                            }
                            else
                            {
                                await _logs.LogsAsync($"Device {imei} lock rejected by Chinese MDM: {lockResponse.Code} - {lockResponse.Message}", 5);
                                await _cliq.SendMessageAsync(EntityId8, Channel,
                                    $"❌ *Device Lock Failed*\n• *SerialNo:* {imei}\n• *Chinese MDM:* {lockResponse.Code} - {TranslateVendorMessage(lockResponse.Message)}").ConfigureAwait(false);
                            }
                        }
                        else if (isIPhone)
                        {
                            // Known iPhone: skip the Android-only endpoint entirely, go straight to Marena.
                            if (androidCode == -1) androidMsg = "Skipped (device is a confirmed iPhone)";
                            if (marenaClearCode == -1)
                                (marenaClearCode, marenaClearMsg) = await _marenaService.ClearLockAsync(imei, entityId).ConfigureAwait(false);

                            if (marenaClearCode == 200)
                            {
                                var lockResponseMarena = await _marenaService.LockPhoneAsync(imei, entityId).ConfigureAwait(false);
                                Console.WriteLine($"Device {imei} locked via Marena → {lockResponseMarena.Code} {lockResponseMarena.Message}");

                                if (lockResponseMarena.Code == 200)
                                {
                                    await _logs.LogsAsync($"Device {imei} locked via Marena successfully.", 5);
                                    locked = true;
                                    winningProvider = "MarenaMDM";
                                }
                                else
                                {
                                    await _logs.LogsAsync($"Device {imei} lock rejected by Marena: {lockResponseMarena.Code} - {lockResponseMarena.Message}", 5);
                                    await _cliq.SendMessageAsync(EntityId8, Channel,
                                        $"❌ *Device Lock Failed*\n• *SerialNo:* {imei}\n• *Marena MDM:* {lockResponseMarena.Code} - {TranslateVendorMessage(lockResponseMarena.Message)}").ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Serial Number {imei} (iPhone) not found in Chinese or Marena MDM. Saving to UnknownDevices.");
                                await _logs.LogsAsync($"Serial Number {imei} (iPhone) does not exist in Chinese or Marena MDM. Saved to UnknownDevices.", 5);
                                await SaveUnknownDeviceAsync(imei, entityId, conn).ConfigureAwait(false);

                                await _cliq.SendMessageAsync(EntityId8, Channel,
                                    BuildNotFoundMessage(imei, chineseClearCode, chineseClearMsg, -1, "", marenaClearCode, marenaClearMsg, isIPhone: true)).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            if (androidCode == -1)
                                (androidCode, androidMsg) = await _chineseService.LockAndroidPhoneAsync(imei, entityId).ConfigureAwait(false);

                            if (androidCode == 200 || androidCode == -1409)
                            {
                                Console.WriteLine($"Device {imei} locked via Chinese MDM Android → ");
                                await _logs.LogsAsync($"Device {imei} locked via Chinese MDM Android.", 5);
                                locked = true;
                                winningProvider = "ChineseMDM-Android";
                            }
                            else
                            {
                                // Step 2: Fallback to Marena MDM
                                if (marenaClearCode == -1)
                                    (marenaClearCode, marenaClearMsg) = await _marenaService.ClearLockAsync(imei, entityId).ConfigureAwait(false);

                                if (marenaClearCode == 200)
                                {
                                    var lockResponseMarena = await _marenaService.LockPhoneAsync(imei, entityId).ConfigureAwait(false);
                                    Console.WriteLine($"Device {imei} locked via Marena → {lockResponseMarena.Code} {lockResponseMarena.Message}");

                                    if (lockResponseMarena.Code == 200)
                                    {
                                        await _logs.LogsAsync($"Device {imei} locked via Marena successfully.", 5);
                                        locked = true;
                                        winningProvider = "MarenaMDM";
                                    }
                                    else
                                    {
                                        await _logs.LogsAsync($"Device {imei} lock rejected by Marena: {lockResponseMarena.Code} - {lockResponseMarena.Message}", 5);
                                        await _cliq.SendMessageAsync(EntityId8, Channel,
                                            $"❌ *Device Lock Failed*\n• *SerialNo:* {imei}\n• *Marena MDM:* {lockResponseMarena.Code} - {lockResponseMarena.Message}").ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Serial Number {imei} not found in any MDM. Saving to UnknownDevices.");
                                    await _logs.LogsAsync($"Serial Number {imei} does not exist in Chinese or Marena MDM. Saved to UnknownDevices.", 5);
                                    await SaveUnknownDeviceAsync(imei, entityId, conn).ConfigureAwait(false);

                                    await _cliq.SendMessageAsync(EntityId8, Channel,
                                        BuildNotFoundMessage(imei, chineseClearCode, chineseClearMsg, androidCode, androidMsg, marenaClearCode, marenaClearMsg, isIPhone: false)).ConfigureAwait(false);
                                }
                            }
                        }
                        }

                        if (locked && winningProvider != null)
                            await SaveKnownProviderAsync(conn, imei, loanId, entityId, winningProvider).ConfigureAwait(false);

                        // Step 3: Mark DB as processed and record lock time in history.
                        // A rateLimited device (-1400: already has a command queued at Chinese
                        // MDM) is marked processed too, same as a confirmed lock — retrying it
                        // would just stack redundant commands on top of the one already pending.
                        const string updateQuery = "UPDATE PhoneLockRequests SET IsProcessed = 1 WHERE LTRIM(RTRIM(SerialNo)) = @IMEI";
                        await using var updateCmd = new SqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@IMEI", imei);
                        await updateCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

                        if (locked)
                        {
                            await SaveLockHistoryAsync(imei, entityId, conn).ConfigureAwait(false);
                            _cumulativeLockedCount++;
                        }

                        Console.WriteLine($"IMEI {imei} marked as processed.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing IMEI {imei}: {ex.Message}");
                        await _logs.LogsAsync($"Error processing IMEI {imei}: {ex.Message}", 5);
                        await _cliq.SendMessageAsync(EntityId8, Channel,
                            $"❌ *Device Lock Failed*\n• *SerialNo:* {imei}\n• *Error:* {ex.Message}").ConfigureAwait(false);
                    }

                    // Pace requests to avoid tripping the MDM's rate limit (-1400: too many commands in flight).
                    await Task.Delay(1000).ConfigureAwait(false);
                }

                Console.WriteLine("✅ All devices processed successfully.");

                const string remainingQuery = @"
                    SELECT COUNT(*)
                    FROM PhoneLockRequests p
                    WHERE p.Locktypeid = 1
                      AND p.IsProcessed = 0
                      AND NOT EXISTS (SELECT 1 FROM UnknownDevices u WHERE u.SerialNo = p.SerialNo)";
                int remaining;
                await using (var remainingCmd = new SqlCommand(remainingQuery, conn))
                {
                    remaining = (int)await remainingCmd.ExecuteScalarAsync().ConfigureAwait(false);
                }

                if (remaining == 0 && _cumulativeLockedCount > 0)
                {
                    await _cliq.SendMessageAsync(EntityId8, Channel,
                        $"🔒 *Phone Locking Completed*\n• *Total Phones Locked Successfully:* {_cumulativeLockedCount}").ConfigureAwait(false);
                    _cumulativeLockedCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessLockPhoneAsync] Fatal Error: {ex.Message}");
                await _logs.LogsAsync($"Fatal Error in processing phones: {ex.Message}", 5);
            }
        }

        // Known Chinese MDM / Marena response messages we've seen, translated to English so
        // Cliq notifications are readable without needing a translator. Unrecognized messages
        // pass through unchanged in TranslateVendorMessage rather than being hidden, so a
        // message we haven't catalogued yet still reaches the channel.
        private static readonly Dictionary<string, string> _vendorMessageTranslations = new()
        {
            ["等待响应的命令过多，请稍后再试"] = "Too many commands awaiting response, try again later",
            ["找不到设备"] = "Device not found",
            ["命令下发失败：设备已锁定"] = "Command delivery failed — device is already locked",
            ["没有操作权限"] = "No operation permission",
            ["请勿重复下发相同的命令"] = "Please do not resend the same command repeatedly",
            ["命令下发成功"] = "Command delivered successfully",
            ["设备不存在"] = "Device does not exist",
        };

        /// <summary>Translates a known vendor response message to English for Cliq notifications.</summary>
        private static string TranslateVendorMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            return _vendorMessageTranslations.TryGetValue(raw.Trim(), out var translated) ? translated : raw;
        }

        /// <summary>
        /// Builds the Cliq message for the terminal "couldn't lock anywhere" case. Only when
        /// both providers report their specific "not found" codes (-1404 Chinese / 8000 Marena)
        /// do we say the device is genuinely unregistered — any other code (e.g. -409 "no
        /// operation permission") is a distinct, real problem and must not be mislabeled as
        /// "not found", so it falls through to the raw code dump instead.
        /// </summary>
        private static string BuildNotFoundMessage(string imei, int chineseCode, string chineseMsg, int androidCode, string androidMsg, int marenaCode, string marenaMsg, bool isIPhone)
        {
            bool chineseNotFound = chineseCode == -1404 || androidCode == -1404;
            bool marenaNotFound = marenaCode == 8000;

            if (chineseNotFound && marenaNotFound)
            {
                return $"❌ *Device Not Found*\n• *SerialNo:* {imei}\n• *Reason:* This device is not registered in either Chinese MDM or Marena MDM. Verify the serial number is correct or check that the device was properly enrolled.";
            }

            chineseMsg = TranslateVendorMessage(chineseMsg);
            androidMsg = TranslateVendorMessage(androidMsg);
            marenaMsg = TranslateVendorMessage(marenaMsg);

            string reason = isIPhone
                ? $"• *Chinese MDM:* {chineseCode} - {chineseMsg}\n• *Marena MDM:* {marenaCode} - {marenaMsg}"
                : $"• *Chinese MDM:* {chineseCode} - {chineseMsg}\n• *Chinese MDM Android:* {androidCode} - {androidMsg}\n• *Marena MDM:* {marenaCode} - {marenaMsg}";
            return $"❌ *Device Lock Failed*\n• *SerialNo:* {imei}\n{reason}";
        }

        /// <summary>Looks up the MDM provider that last successfully unlocked/locked this device, if known.</summary>
        private async Task<string> GetKnownProviderAsync(SqlConnection conn, string serialNo)
        {
            const string query = "SELECT Provider FROM DeviceMdmProvider WHERE SerialNo = @SerialNo";
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@SerialNo", serialNo);
            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return result as string;
        }

        /// <summary>Remembers which MDM provider worked for this device, so future attempts skip the guesswork.</summary>
        private async Task SaveKnownProviderAsync(SqlConnection conn, string serialNo, int? loanId, int entityId, string provider)
        {
            const string query = @"
                MERGE DeviceMdmProvider AS target
                USING (SELECT @SerialNo AS SerialNo) AS source ON target.SerialNo = source.SerialNo
                WHEN MATCHED THEN
                    UPDATE SET Provider = @Provider, LoanId = @LoanId, EntityId = @EntityId, LastConfirmedAt = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (SerialNo, LoanId, EntityId, Provider, LastConfirmedAt)
                    VALUES (@SerialNo, @LoanId, @EntityId, @Provider, GETDATE());";
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@SerialNo", serialNo);
            cmd.Parameters.AddWithValue("@LoanId", (object)loanId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EntityId", entityId);
            cmd.Parameters.AddWithValue("@Provider", provider);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task SaveLockHistoryAsync(string serialNo, int entityId, SqlConnection conn)
        {
            const string query = @"
                MERGE DeviceLockHistory AS target
                USING (SELECT @SerialNo AS SerialNo) AS source ON target.SerialNo = source.SerialNo
                WHEN MATCHED THEN
                    UPDATE SET LastLockedAt = GETDATE(), EntityId = @EntityId, LockedBy = 'IphoneLocker'
                WHEN NOT MATCHED THEN
                    INSERT (SerialNo, EntityId, LastLockedAt, LockedBy)
                    VALUES (@SerialNo, @EntityId, GETDATE(), 'IphoneLocker');";
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@SerialNo", serialNo);
            cmd.Parameters.AddWithValue("@EntityId", entityId);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task SaveUnknownDeviceAsync(string serialNo, int entityId, SqlConnection conn)
        {
            const string query = @"
                IF NOT EXISTS (SELECT 1 FROM UnknownDevices WHERE SerialNo = @SerialNo)
                    INSERT INTO UnknownDevices (SerialNo, EntityId, DateAdded)
                    VALUES (@SerialNo, @EntityId, GETDATE())";
            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@SerialNo", serialNo);
            cmd.Parameters.AddWithValue("@EntityId", entityId);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }
}

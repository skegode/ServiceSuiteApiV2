using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TrustonicLockingApp.Models;
using static System.Reflection.Metadata.BlobBuilder;

namespace TrustonicLockingApp
{
    internal class NouvoPay
    {
        private const string ApiToken = "b3c6a870ef244f6f85d4974d9d477131";
        private const string BaseUrl = "https://api.nuovopay.com/dm/api";

        private readonly string _constr =
            System.Configuration.ConfigurationManager.ConnectionStrings["constr"].ConnectionString;

        private readonly Logs _logs = new Logs();
        private readonly ZohoCliqNotifier _cliq = new ZohoCliqNotifier();


        /// <summary>
        /// Unlocks devices that are fully paid and marks them as processed.
        /// </summary>
        public async Task UnlockPaidClientsAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_constr))
                {
                    await conn.OpenAsync();

                    string selectQuery = @"
                        SELECT TOP 10 p.ID, p.IMEI, p.EntityId, p.channelName,
                               LTRIM(RTRIM(ISNULL(b.firstName,'') + ' ' + ISNULL(b.otherName,''))) AS CustomerName,
                               b.PhoneNumber
                        FROM PhoneUnlockRequests p
                        LEFT JOIN Loans l ON l.ID = p.LoanId
                        LEFT JOIN Borrowers b ON b.ID = l.BorrowerId
                        WHERE p.Locktypeid=2
                          AND p.Arrears = 0
                          AND p.IsNouvoPayProcessed = 0";

                    // === Step 1: Read data into memory ===
                    var unlockList = new List<(int RequestId, string Imei, int EntityId, string ChannelName, string CustomerName, string PhoneNumber)>();

                    using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            unlockList.Add((
                                reader.GetInt32(0),
                                reader.GetString(1),
                                reader.GetInt32(2),
                                reader.IsDBNull(3) ? null : reader.GetString(3),
                                reader.IsDBNull(4) ? "" : reader.GetString(4),
                                reader.IsDBNull(5) ? "" : reader.GetString(5)
                            ));
                        }
                    }

                    if (unlockList.Count == 0)
                    {
                        Console.WriteLine("No pending unlock requests found.");
                        return;
                    }

                    // === Step 2: Process each IMEI ===
                    foreach (var (requestId, imei, entityId, channelName, customerName, phoneNumber) in unlockList)
                    {
                        Console.WriteLine($"Processing IMEI: {imei}");

                        try
                        {
                            DateTime futureDate = DateTime.UtcNow.AddDays(360);

                            string response = await UpdateDeviceUnlockDateAsync(imei, futureDate);
                            Console.WriteLine($"IMEI {imei} → {response}");

                            await MarkAsProcessed(conn, requestId);

                            if (response.StartsWith("Device unlocked successfully") || response.Contains("already unlocked"))
                            {
                                await _cliq.SendMessageAsync(entityId, channelName,
                                    $"✅ *Device Unlocked Successfully*\n• *Customer:* {customerName}\n• *Phone:* {phoneNumber}\n• *Arrears Balance:* 0.00\n• *IMEI:* {imei}\n• *Unlocked via:* NuovoPay");
                            }
                            else if (response.Contains("No device found"))
                            {
                                await _cliq.SendMessageAsync(entityId, channelName,
                                    $"❌ *Device Not Found in NuovoPay*\n• *Customer:* {customerName}\n• *Phone:* {phoneNumber}\n• *Arrears Balance:* 0.00\n• *IMEI:* {imei}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing IMEI {imei}: {ex.Message}");
                        }
                    }

                    Console.WriteLine("✅ All paid client devices processed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UnlockPaidClients] Fatal Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Locks devices for unpaid clients and marks them as processed.
        /// </summary>
        public async Task lockUnPaidClientsAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_constr))
                {
                    await conn.OpenAsync();

                    // === Step 1: Get comma-separated IMEIs ===
                    string selectQuery = @"
                SELECT STRING_AGG(IMEI, ',')
                FROM (
                    SELECT TOP 50 IMEI
                    FROM PhoneLockRequests
                    WHERE IsProcessed = 0 AND Locktypeid=2
                ) x;";

                    string imeiList = null;
                    using (SqlCommand cmd = new SqlCommand(selectQuery, conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        imeiList = result?.ToString();
                    }

                    if (string.IsNullOrEmpty(imeiList))
                    {
                        Console.WriteLine("No unpaid clients found to lock.");
                        return;
                    }

                    string[] imeis = imeiList.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    Console.WriteLine($"Fetched {imeis.Length} IMEIs to process.");

                    // === Step 2: Process each IMEI ===
                    foreach (var imei in imeis)
                    {
                        string trimmedImei = imei.Trim();
                        if (string.IsNullOrWhiteSpace(trimmedImei))
                            continue;

                        Console.WriteLine($"Processing IMEI: {trimmedImei}");

                        try
                        {
                            string response = await UpdateDevicelockDateAsync(trimmedImei);
                            Console.WriteLine($"IMEI {trimmedImei} → {response}");

                            string updateQuery = "UPDATE PhoneLockRequests SET IsProcessed = 1 WHERE LTRIM(RTRIM(IMEI)) = @IMEI";
                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@IMEI", trimmedImei);
                                await updateCmd.ExecuteNonQueryAsync();
                            }

                            Console.WriteLine($"IMEI {trimmedImei} marked as processed.");

                            // Small delay to prevent API overload
                            await Task.Delay(2000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error locking IMEI {trimmedImei}: {ex.Message}");
                        }
                    }

                    Console.WriteLine("✅ All unpaid client devices processed successfully.");

               
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[lockUnPaidClients] Fatal Error: {ex.Message}");
            }
        }

        public async Task TruncatePhoneLockRequestsAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_constr))
                {
                    await conn.OpenAsync();

                    // IphoneLocker only processes TOP 50 unprocessed rows per worker cycle.
                    // Truncating unconditionally here would wipe out any rows still queued
                    // (IsProcessed = 0) beyond that batch before they ever get a chance to
                    // run, silently dropping the rest of the day's lock list. Only clear the
                    // table once nothing is left pending.
                    string pendingQuery = "SELECT COUNT(*) FROM PhoneLockRequests WHERE IsProcessed = 0;";
                    int pending;
                    using (SqlCommand pendingCmd = new SqlCommand(pendingQuery, conn))
                    {
                        pending = (int)await pendingCmd.ExecuteScalarAsync();
                    }

                    if (pending > 0)
                    {
                        Console.WriteLine($"⏳ Skipping truncate of 'PhoneLockRequests' — {pending} row(s) still pending.");
                        return;
                    }

                    string truncateQuery = "TRUNCATE TABLE PhoneLockRequests;";

                    using (SqlCommand truncateCmd = new SqlCommand(truncateQuery, conn))
                    {
                        await truncateCmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine("🧹 Table 'PhoneLockRequests' truncated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error truncating table PhoneLockRequests: {ex.Message}");
            }
        }



        private async Task MarkAsProcessed(SqlConnection conn, int id)
        {
            string updateQuery = "UPDATE PhoneUnlockRequests SET IsNouvoPayProcessed = 1 WHERE ID = @Id";

            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
            {
                updateCmd.Parameters.AddWithValue("@Id", id);
                await updateCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Unlocks a device by setting a new auto-lock date on the NouvoPay API.
        /// </summary>
        public async Task<string> UpdateDeviceUnlockDateAsync(string imei, DateTime autoLockDate)
        {
            await _logs.LogsAsync(imei, 4);

            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            {
                // === Step 1: Check device status ===
                var checkUrl = $"{BaseUrl}/v1/devices.json?search_string={imei}";

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", ApiToken);

                var checkResponse = await client.GetAsync(checkUrl);
                string checkContent = await checkResponse.Content.ReadAsStringAsync();

                if (!checkResponse.IsSuccessStatusCode)
                    throw new Exception($"Device check failed: {checkResponse.StatusCode} - {checkContent}");

                dynamic checkData = JsonConvert.DeserializeObject(checkContent);
                if (checkData.devices.Count == 0)
                    return $"No device found with IMEI: {imei}";

                var device = checkData.devices[0];
                bool isLocked = device.locked;
                int deviceId = device.id;

                if (!isLocked)
                    return $"Device {device.name} (IMEI: {imei}) is already unlocked.";


                // === Step 2: Unlock if locked ===
                var unlockUrl = $"{BaseUrl}/v3/devices/unlock.json";
                var requestBody = new
                {
                    data = new[]
                    {
                        new
                        {
                            device_id = deviceId,
                            auto_lock_date = autoLockDate.ToString("yyyy-MM-dd")
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var method = new HttpMethod("PATCH");
                var request = new HttpRequestMessage(method, unlockUrl) { Content = content };

                var unlockResponse = await client.SendAsync(request);
                string unlockContent = await unlockResponse.Content.ReadAsStringAsync();

                if (!unlockResponse.IsSuccessStatusCode)
                    throw new Exception($"Unlock request failed: {unlockResponse.StatusCode} - {unlockContent}");

                return $"Device unlocked successfully. Response: {unlockContent}";
            }
        }

        /// <summary>
        /// Locks a device if not already locked via NouvoPay API.
        /// </summary>
        public async Task<string> UpdateDevicelockDateAsync(string imei)
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            {
                // === Step 1: Check device status ===
                var checkUrl = $"{BaseUrl}/v1/devices.json?search_string={imei}";

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", ApiToken);

                var checkResponse = await client.GetAsync(checkUrl);
                string checkContent = await checkResponse.Content.ReadAsStringAsync();

                if (!checkResponse.IsSuccessStatusCode)
                    throw new Exception($"Device check failed: {checkResponse.StatusCode} - {checkContent}");

                dynamic checkData = JsonConvert.DeserializeObject(checkContent);
                if (checkData.devices.Count == 0)
                    return $"No device found with the given IMEI: {imei}";

                var device = checkData.devices[0];
                bool isLocked = device.locked;
                int deviceId = device.id;

                // === Step 2: Lock if not already locked ===
                if (!isLocked)
                {
                    var lockUrl = $"{BaseUrl}/v1/devices/lock.json";

                    var formData = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("device_ids[]", deviceId.ToString())
                    };

                    var request = new HttpRequestMessage(HttpMethod.Patch, lockUrl)
                    {
                        Content = new FormUrlEncodedContent(formData)
                    };

                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", ApiToken);

                    var lockResponse = await client.SendAsync(request);
                    string lockContent = await lockResponse.Content.ReadAsStringAsync();

                    if (!lockResponse.IsSuccessStatusCode)
                        throw new Exception($"Lock request failed: {lockResponse.StatusCode} - {lockContent}");

                    string successMsg = $"Device locked successfully (IMEI: {imei}). Response: {lockContent}";
                    await _logs.LogsAsync(successMsg, 4);

                    Console.WriteLine(successMsg);
                    return successMsg;
                }
                else
                {
                    string msg = $"Device {imei} is already locked. No action taken.";
                    Console.WriteLine(msg);
                    await _logs.LogsAsync(msg, 3);
                    return msg;
                }
            }
        }
    }
}

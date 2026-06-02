using Dapper;
using Microsoft.Data.SqlClient;

namespace ServiceSuiteApiV2
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _config;
        private string ConnectionString => _config.GetConnectionString("DefaultConnection")!;

        public SmsService(IConfiguration config) => _config = config;

        public async Task<(bool Success, string Message)> SendSmsAsync(
            string message, string phoneNumber, int entityId, DateTime scheduleDate)
        {
            await using var conn = new SqlConnection(ConnectionString);

            var units = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT Units FROM Notifications.dbo.EMalifyParameters WHERE EntityId = @EntityId",
                new { EntityId = entityId });

            if (units == null || units <= 5)
                return (false, "Insufficient balance.");

            var smsProviderId = await conn.QueryFirstOrDefaultAsync<int?>(
                "SELECT SmsProviderId FROM BsEntity WHERE ID = @EntityId",
                new { EntityId = entityId });

            const string insertSql = """
                INSERT INTO Notifications.DBO.SMS
                    (smsMessage, smsto, EntityId, CreateDate, CreatedBy, isSent, SmsProviderId, ScheduleDate)
                VALUES
                    (@Message, @PhoneNumber, @EntityId, GETDATE(), 101, 0, @SmsProviderId, @ScheduleDate)
                """;

            await conn.ExecuteAsync(insertSql, new
            {
                Message       = message,
                PhoneNumber   = phoneNumber,
                EntityId      = entityId,
                SmsProviderId = smsProviderId ?? 0,
                ScheduleDate  = scheduleDate
            });

            return (true, "SMS queued successfully.");
        }
    }
}

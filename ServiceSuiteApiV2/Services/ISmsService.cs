namespace ServiceSuiteApiV2
{
    public interface ISmsService
    {
        Task<(bool Success, string Message)> SendSmsAsync(string message, string phoneNumber, int entityId, DateTime scheduleDate);
    }
}

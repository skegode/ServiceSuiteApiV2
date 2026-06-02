namespace ServiceSuiteApiV2
{
    public interface IStkService
    {
        Task<string> InitiateStkPushAsync(
            decimal amount,
            string phoneNumber,
            int entityId,
            string? accountReference = null,
            string transactionDesc = "Payment");
    }
}

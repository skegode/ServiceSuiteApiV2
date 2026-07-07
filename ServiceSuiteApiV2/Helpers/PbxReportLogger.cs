namespace ServiceSuiteApiV2.Helpers
{
    public static class PbxReportLogger
    {
        public static async Task WriteLogAsync(string rawJson)
        {
            var dir = @"C:\Logs\PbxReports";
            Directory.CreateDirectory(dir);

            var filePath = Path.Combine(dir, $"Log-{DateTime.Now:yyyy-MM-dd}.txt");
            var entry = $"{DateTime.Now:O} {rawJson}{Environment.NewLine}";

            await File.AppendAllTextAsync(filePath, entry);
        }
    }
}

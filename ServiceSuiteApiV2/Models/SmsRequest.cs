namespace ServiceSuiteApiV2.Models
{
    public class SmsRequest
    {
        public string   Message      { get; set; } = "";
        public string   PhoneNumber  { get; set; } = "";
        public DateTime ScheduleDate { get; set; }
    }
}

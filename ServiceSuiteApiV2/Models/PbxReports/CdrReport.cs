namespace ServiceSuiteApiV2.Models.PbxReports
{
    public class CdrReport
    {
        public string? callid { get; set; }
        public string? timestart { get; set; }
        public string? callfrom { get; set; }
        public string? callto { get; set; }
        public string? callduraction { get; set; }
        public string? talkduraction { get; set; }
        public string? srctrunkname { get; set; }
        public string? dsttrcunkname { get; set; }
        public string? pincode { get; set; }
        public string? status { get; set; }
        public string? type { get; set; }
        public string? recording { get; set; }
        public string? didnumber { get; set; }
        public string? agentringtime { get; set; }
        public string? sn { get; set; }
    }
}

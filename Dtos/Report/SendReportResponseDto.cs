namespace DeviceManagement.Dtos.Report
{
    public class SendReportResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? ReportLogId { get; set; }
    }
}

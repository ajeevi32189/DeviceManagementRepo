namespace DeviceManagement.Services.Report
{
    public interface IEmailService
    {
        Task SendReportEmailAsync(string toEmail, string subject, byte[] pdfBytes, string attachmentFileName);
    }
}

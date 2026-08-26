using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DeviceManagement.Services.Report
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendReportEmailAsync(string toEmail, string subject, byte[] pdfBytes, string attachmentFileName)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"] ?? throw new InvalidOperationException("Smtp:Host not configured.");
            var port = int.Parse(smtpSection["Port"] ?? "587");
            var username = smtpSection["Username"] ?? throw new InvalidOperationException("Smtp:Username not configured.");
            var password = smtpSection["Password"] ?? throw new InvalidOperationException("Smtp:Password not configured.");
            var fromName = smtpSection["FromName"] ?? "Ajeevi DCIM Reports";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody =
                    "<p>Hello,</p>" +
                    "<p>Please find attached the requested calculator report from the Ajeevi DCIM " +
                    "Data Centre Calculator Tools module.</p>" ,
                   // "<p style='color:#5A6B72;font-size:12px;'>This is an automated message from Ajeevi DCIM.</p>",
            };
            builder.Attachments.Add(attachmentFileName, pdfBytes, new ContentType("application", "pdf"));
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(username, password);
                await smtp.SendAsync(message);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }

            _logger.LogInformation("Report email sent to {Email}", toEmail);
        }
    }
}

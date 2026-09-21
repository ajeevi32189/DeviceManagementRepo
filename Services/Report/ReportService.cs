using System.Text.Json;

using DeviceManagement.Dtos.Report;
using DeviceManagement.Models.Report;
using DeviceManagement.Data;

namespace DeviceManagement.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly IDocxReportBuilder _docxBuilder;
        private readonly IPdfConverterService _pdfConverter;
        private readonly IEmailService _emailService;
        private readonly DBContext _db;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            IDocxReportBuilder docxBuilder,
            IPdfConverterService pdfConverter,
            IEmailService emailService,
            DBContext db,
            ILogger<ReportService> logger)
        {
            _docxBuilder = docxBuilder;
            _pdfConverter = pdfConverter;
            _emailService = emailService;
            _db = db;
            _logger = logger;
        }

        public async Task<SendReportResponseDto> GenerateAndSendReportAsync(SendReportRequestDto request, string? requestedBy)
        {
            var log = new ReportLog
            {
                CalculatorId = request.CalculatorId,
                CalculatorName = request.CalculatorName,
                RecipientEmail = request.Email,
                PayloadJson = JsonSerializer.Serialize(request),
                RequestedBy = requestedBy,
                Status = "Pending",
            };
            _db.ReportLogs.Add(log);
            await _db.SaveChangesAsync();

            try
            {
                var docxBytes = _docxBuilder.BuildReport(request);
                var pdfBytes = await _pdfConverter.ConvertDocxToPdfAsync(docxBytes);

                var fileName = $"{request.CalculatorName.Replace(" ", "_")}_Report.pdf";
                var subject = $"{request.CalculatorName} Report — Ajeevi DCIM";

                await _emailService.SendReportEmailAsync(request.Email, subject, pdfBytes, fileName);

                log.Status = "Sent";
                log.SentAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return new SendReportResponseDto
                {
                    Success = true,
                    Message = $"Report sent to {request.Email}.",
                    ReportLogId = log.Id,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate/send report for calculator {CalculatorId}", request.CalculatorId);
                log.Status = "Failed";
                log.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                await _db.SaveChangesAsync();

                return new SendReportResponseDto
                {
                    Success = false,
                    Message = "We couldn't generate or send the report. Please try again shortly.",
                    ReportLogId = log.Id,
                };
            }
        }

        public async Task<(byte[] PdfBytes, string FileName)> GeneratePdfAsync(GeneratePdfRequestDto request)
        {
            // DocxReportBuilder ko same shape chahiye — Email field ka use nahi hoga isme
            var buildRequest = new SendReportRequestDto
            {
                CalculatorId = request.CalculatorId,
                CalculatorName = request.CalculatorName,
                Formula = request.Formula,
                Inputs = request.Inputs,
                Result = request.Result,
                Email = "n/a"
            };

            var docxBytes = _docxBuilder.BuildReport(buildRequest);
            var pdfBytes = await _pdfConverter.ConvertDocxToPdfAsync(docxBytes);
            var fileName = $"{request.CalculatorName.Replace(" ", "_")}_Report.pdf";

            // Audit trail yaha bhi — email-send ke ReportLogs ke saath consistent
            _db.ReportLogs.Add(new ReportLog
            {
                CalculatorId = request.CalculatorId,
                CalculatorName = request.CalculatorName,
                RecipientEmail = "N/A (PDF download)",
                PayloadJson = JsonSerializer.Serialize(request),
                Status = "Downloaded",
                SentAtUtc = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();

            return (pdfBytes, fileName);
        }
    }
}

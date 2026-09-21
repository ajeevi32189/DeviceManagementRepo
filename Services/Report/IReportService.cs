using DeviceManagement.Dtos.Report;

namespace DeviceManagement.Services.Report
{
    public interface IReportService
    {
        Task<SendReportResponseDto> GenerateAndSendReportAsync(SendReportRequestDto request, string? requestedBy);

        // NEW — sirf PDF banao, email mat bhejo
        Task<(byte[] PdfBytes, string FileName)> GeneratePdfAsync(GeneratePdfRequestDto request);
    }
}
using DeviceManagement.Dtos.Report;

namespace DeviceManagement.Services.Report
{
    public interface IDocxReportBuilder
    {
        /// <summary>
        /// Builds the Word report (intro + calculator explanation + input/result table +
        /// recommendations, with the Ajeevi logo in the header/footer) and returns the
        /// raw .docx bytes.
        /// </summary>
        byte[] BuildReport(SendReportRequestDto request);
    }
}

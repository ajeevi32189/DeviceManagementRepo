namespace DeviceManagement.Services.Report
{
    public interface IPdfConverterService
    {
        /// <summary>Converts .docx bytes to .pdf bytes using headless LibreOffice.</summary>
        Task<byte[]> ConvertDocxToPdfAsync(byte[] docxBytes);
    }
}

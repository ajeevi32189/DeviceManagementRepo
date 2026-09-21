using System.Diagnostics;

namespace DeviceManagement.Services.Report
{
    /// <summary>
    /// Converts .docx to .pdf by shelling out to headless LibreOffice ("soffice").
    /// This keeps the pipeline 100% free/open-source (no Aspose/Syncfusion licence needed).
    ///
    /// IMPORTANT: The container/host this runs on must have LibreOffice installed.
    /// For the existing Dockerfile (mcr.microsoft.com/dotnet/aspnet:8.0 base), add:
    ///     RUN apt-get update && apt-get install -y libreoffice --no-install-recommends \
    ///         && rm -rf /var/lib/apt/lists/*
    /// </summary>
    public class PdfConverterService : IPdfConverterService
    {
        private readonly ILogger<PdfConverterService> _logger;

        public PdfConverterService(ILogger<PdfConverterService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> ConvertDocxToPdfAsync(byte[] docxBytes)
        {
            var workDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDir);
            var docxPath = Path.Combine(workDir, "report.docx");
            var pdfPath = Path.Combine(workDir, "report.pdf");

            try
            {
                await File.WriteAllBytesAsync(docxPath, docxBytes);

                var psi = new ProcessStartInfo
                {
                    FileName = "soffice",
                    Arguments = $"--headless --norestore --convert-to pdf --outdir \"{workDir}\" \"{docxPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(psi)
                    ?? throw new InvalidOperationException("Failed to start soffice process.");

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                var completed = await Task.Run(() => process.WaitForExit(30000)); // 30s timeout
                if (!completed)
                {
                    process.Kill(true);
                    throw new TimeoutException("LibreOffice PDF conversion timed out.");
                }

                if (process.ExitCode != 0 || !File.Exists(pdfPath))
                {
                    var stderr = await stdErrTask;
                    _logger.LogError("soffice conversion failed. ExitCode={ExitCode} Stderr={Stderr}", process.ExitCode, stderr);
                    throw new InvalidOperationException("PDF conversion failed. See logs for details.");
                }

                return await File.ReadAllBytesAsync(pdfPath);
            }
            finally
            {
                try { Directory.Delete(workDir, recursive: true); } catch { /* best-effort cleanup */ }
            }
        }
    }
}

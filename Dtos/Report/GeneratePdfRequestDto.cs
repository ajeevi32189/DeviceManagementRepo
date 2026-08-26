using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Dtos.Report
{
    /// <summary>
    /// Body of POST /api/reports/generate-pdf — same shape as SendReportRequestDto
    /// but WITHOUT email (used for the "Save as PDF" button, no sending involved).
    /// </summary>
    public class GeneratePdfRequestDto
    {
        [Required]
        public string CalculatorId { get; set; } = string.Empty;

        [Required]
        public string CalculatorName { get; set; } = string.Empty;

        [Required]
        public string Formula { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one input value is required.")]
        public List<CalculatorInputDto> Inputs { get; set; } = new();

        [Required]
        public CalculatorResultDto Result { get; set; } = new();
    }
}
using System.ComponentModel.DataAnnotations;

namespace DeviceManagement.Dtos.Report
{
    /// <summary>
    /// Body of POST /api/reports/send — sent by the "Send Report" button in the
    /// Data Centre Calculator Tools HTML page.
    /// </summary>
    public class SendReportRequestDto
    {
        [Required]
        public string CalculatorId { get; set; } = string.Empty;      // e.g. "power", "ups", "pue"

        [Required]
        public string CalculatorName { get; set; } = string.Empty;    // e.g. "Power Load Calculator"

        [Required]
        public string Formula { get; set; } = string.Empty;

        [Required]
        [MinLength(1, ErrorMessage = "At least one input value is required.")]
        public List<CalculatorInputDto> Inputs { get; set; } = new();

        [Required]
        public CalculatorResultDto Result { get; set; } = new();

        [Required]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        public string Email { get; set; } = string.Empty;
    }
}

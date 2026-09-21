using DeviceManagement.Dtos.Report;
using DeviceManagement.Services.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a Word/PDF report for the given calculator and emails it to the
        /// requested address. Called by the "Send Report" button in the
        /// Data Centre Calculator Tools HTML page.
        /// </summary>
        [HttpPost("send")]
        [AllowAnonymous] // NOTE: remove this and add [Authorize] once the calculator page sits behind login.
        [ProducesResponseType(typeof(SendReportResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SendReportResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SendReportResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendReport([FromBody] SendReportRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new SendReportResponseDto
                {
                    Success = false,
                    Message = "Invalid request: " + string.Join("; ",
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                });
            }

            // If [Authorize] is enabled, pull the caller's identity from the JWT instead:
            // var requestedBy = User.Identity?.Name;
            string? requestedBy = null;

            var result = await _reportService.GenerateAndSendReportAsync(request, requestedBy);

            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }

            return Ok(result);
        }

        [HttpPost("generate-pdf")]
        [AllowAnonymous]
        public async Task<IActionResult> GeneratePdf([FromBody] GeneratePdfRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request." });
            }

            try
            {
                var (pdfBytes, fileName) = await _reportService.GeneratePdfAsync(request);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate PDF for calculator {CalculatorId}", request.CalculatorId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Couldn't generate the PDF. Please try again." });
            }
        }
    }
}

namespace DeviceManagement.Dtos.Report
{
    /// <summary>
    /// The computed result shown on the calculator card (e.g. "775", "kW", "IT 400 + Cooling 350...").
    /// </summary>
    public class CalculatorResultDto
    {
        public string Value { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}

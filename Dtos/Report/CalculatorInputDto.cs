namespace DeviceManagement.Dtos.Report
{
    /// <summary>
    /// One input field from a calculator card (e.g. "IT Equipment Load", 400, "kW").
    /// </summary>
    public class CalculatorInputDto
    {
        public string Label { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}

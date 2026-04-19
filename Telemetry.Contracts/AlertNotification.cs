namespace Telemetry.Contracts;

public sealed class AlertNotification
{
    public string Source { get; set; } = "InfluxDB";
    public string RuleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Measurement { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double? Value { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

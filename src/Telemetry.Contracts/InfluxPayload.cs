namespace Telemetry.Contracts;

public sealed class InfluxPayload
{
    public string Measurement { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string Checksum { get; set; } = string.Empty;
}

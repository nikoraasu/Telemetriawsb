namespace Telemetry.Contracts;

public sealed class TelemetryRequest
{
    public string? Channel { get; set; }
    public string? PayloadBase64 { get; set; }
    public string? CorrelationId { get; set; }
}

namespace Telemetry.Contracts;

public sealed class TelemetryEnvelope
{
    public string Channel { get; set; } = string.Empty;
    public string PayloadBase64 { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public string? SourceIp { get; set; }
}

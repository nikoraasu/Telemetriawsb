using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Telemetry.Contracts;
using Telemetry.FrontApi.Services;

namespace Telemetry.FrontApi.Controllers;

[ApiController]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(RabbitMqPublisher publisher, IConfiguration configuration, ILogger<TelemetryController> logger)
    {
        _publisher = publisher;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("{channel?}")]
    public async Task<IActionResult> PostAsync([FromRoute] string? channel, [FromBody] TelemetryRequest request, CancellationToken cancellationToken)
    {
        var effectiveChannel = string.IsNullOrWhiteSpace(channel) ? request.Channel : channel;
        if (string.IsNullOrWhiteSpace(effectiveChannel))
        {
            return BadRequest(new { error = "Channel is required." });
        }

        if (string.IsNullOrWhiteSpace(request.PayloadBase64))
        {
            return BadRequest(new { error = "payloadBase64 is required." });
        }

        byte[] decodedBytes;
        try
        {
            decodedBytes = Convert.FromBase64String(request.PayloadBase64);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "PayloadBase64 is not valid Base64." });
        }

        var decodedText = System.Text.Encoding.UTF8.GetString(decodedBytes);
        _logger.LogInformation("Decoded payload for channel {Channel}: {Payload}", effectiveChannel, decodedText);

        // API only validates that Base64 can be decoded and JSON can be parsed.
        // The integrity checksum is verified later by the Worker.
        using var _ = JsonDocument.Parse(decodedText);

        var envelope = new TelemetryEnvelope
        {
            Channel = effectiveChannel,
            PayloadBase64 = request.PayloadBase64!,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId!,
            ReceivedAtUtc = DateTime.UtcNow,
            SourceIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _publisher.PublishAsync(envelope, cancellationToken);

        return Accepted(new
        {
            message = "Telemetry accepted and queued.",
            envelope.CorrelationId,
            envelope.Channel,
            envelope.ReceivedAtUtc
        });
    }
}

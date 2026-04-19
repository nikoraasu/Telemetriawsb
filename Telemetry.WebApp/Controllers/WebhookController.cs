using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Telemetry.Contracts;
using Telemetry.WebApp.Hubs;

namespace Telemetry.WebApp.Controllers;

[ApiController]
[Route("api/webhook")]
public sealed class WebhookController : ControllerBase
{
    private readonly IHubContext<AlertHub> _hubContext;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IHubContext<AlertHub> hubContext, ILogger<WebhookController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("influx")]
    public async Task<IActionResult> PostAsync([FromBody] JsonElement payload)
    {
        var alert = new AlertNotification
        {
            RuleName = GetString(payload, "ruleName") ?? GetString(payload, "check_name") ?? "Influx alert",
            Message = GetString(payload, "message") ?? GetString(payload, "summary") ?? payload.ToString(),
            Measurement = GetString(payload, "measurement") ?? GetString(payload, "check_name") ?? string.Empty,
            Location = GetString(payload, "location") ?? string.Empty,
            TimestampUtc = DateTime.UtcNow,
            Value = GetDouble(payload, "value")
        };

        _logger.LogInformation("Webhook received: {Message}", alert.Message);

        await _hubContext.Clients.All.SendAsync("ReceiveAlert", alert);
        return Ok(new { status = "received" });
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out var value))
        {
            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        return null;
    }

    private static double? GetDouble(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }

            if (double.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

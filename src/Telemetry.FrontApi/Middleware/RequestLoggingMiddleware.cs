using System.Text;

namespace Telemetry.FrontApi.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        string body = string.Empty;
        if (context.Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        _logger.LogInformation("HTTP {Method} {Path} from {Ip} body={Body}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            body);

        await _next(context);
    }
}

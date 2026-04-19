using Telemetry.FrontApi.Middleware;
using Telemetry.FrontApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Telemetry.FrontApi" }));

app.Run();

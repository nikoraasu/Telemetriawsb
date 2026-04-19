using Telemetry.WebApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin());
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.MapControllers();
app.MapHub<AlertHub>("/hubs/alerts");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Telemetry.WebApp" }));
app.MapFallbackToFile("index.html");

app.Run();

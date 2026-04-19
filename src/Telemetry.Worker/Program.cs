using Telemetry.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<InfluxWriter>();
builder.Services.AddHostedService<RabbitMqConsumerService>();

var host = builder.Build();
host.Run();

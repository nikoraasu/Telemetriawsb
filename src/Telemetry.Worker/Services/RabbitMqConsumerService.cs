using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Telemetry.Contracts;

namespace Telemetry.Worker.Services;

public sealed class RabbitMqConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly InfluxWriter _influxWriter;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumerService(
        IConfiguration configuration,
        InfluxWriter influxWriter,
        ILogger<RabbitMqConsumerService> logger)
    {
        _configuration = configuration;
        _influxWriter = influxWriter;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitSection = _configuration.GetSection("RabbitMq");

        var factory = new ConnectionFactory
        {
            HostName = rabbitSection["HostName"] ?? "localhost",
            Port = int.TryParse(rabbitSection["Port"], out var port) ? port : 5672,
            UserName = rabbitSection["UserName"] ?? "guest",
            Password = rabbitSection["Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: "telemetry.dlx",
            type: ExchangeType.Direct,
            durable: true
        );

        _channel.QueueDeclare(
            queue: "telemetry.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        _channel.QueueBind(
            queue: "telemetry.dlq",
            exchange: "telemetry.dlx",
            routingKey: "telemetry.dead"
        );

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "telemetry.dlx" },
            { "x-dead-letter-routing-key", "telemetry.dead" }
        };

        _channel.QueueDeclare(
            queue: "telemetry.main",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args
        );

        _channel.ExchangeDeclare(
            exchange: "telemetry.exchange",
            type: ExchangeType.Topic,
            durable: true
        );

        _channel.QueueBind(
            queue: "telemetry.main",
            exchange: "telemetry.exchange",
            routingKey: "telemetry.*"
        );

        _channel.BasicQos(0, prefetchCount: 10, global: false);

        _logger.LogInformation("Connected to RabbitMQ and waiting for messages...");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;

        _channel.BasicConsume(
            queue: "telemetry.main",
            autoAck: false,
            consumer: consumer
        );

        stoppingToken.Register(() =>
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
        });

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
            return;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            var envelope = JsonSerializer.Deserialize<TelemetryEnvelope>(json)
                ?? throw new InvalidOperationException("Invalid envelope");

            var decodedBytes = Convert.FromBase64String(envelope.PayloadBase64);
            var payloadJson = Encoding.UTF8.GetString(decodedBytes);

            var payload = JsonSerializer.Deserialize<InfluxPayload>(payloadJson)
                ?? throw new InvalidOperationException("Invalid payload");

            var secretKey = _configuration["Telemetry:SecretKey"] ?? "";

            if (!ChecksumHelper.VerifyChecksum(payload, secretKey))
            {
                throw new Exception("Checksum validation failed");
            }

            _logger.LogInformation("Checksum OK");

            await _influxWriter.WriteAsync(envelope, payload, CancellationToken.None);

            _channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed → sending to DLQ");

            _channel.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
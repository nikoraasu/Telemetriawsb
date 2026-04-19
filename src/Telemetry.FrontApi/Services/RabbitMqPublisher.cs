using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Telemetry.Contracts;

namespace Telemetry.FrontApi.Services;

public sealed class RabbitMqPublisher : IDisposable
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("RabbitMq");

        var factory = new ConnectionFactory
        {
            HostName = section["HostName"] ?? "localhost",
            Port = int.TryParse(section["Port"], out var port) ? port : 5672,
            UserName = section["UserName"] ?? "guest",
            Password = section["Password"] ?? "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        InitializeTopology();
    }

    private void InitializeTopology()
    {
        using var channel = _connection.CreateModel();

        channel.ExchangeDeclare(QueueNames.Exchange, ExchangeType.Topic, durable: true);
        channel.ExchangeDeclare(QueueNames.DeadLetterExchange, ExchangeType.Direct, durable: true);

        var mainArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = QueueNames.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = QueueNames.DeadLetterRoutingKey
        };

        channel.QueueDeclare(QueueNames.MainQueue, durable: true, exclusive: false, autoDelete: false, arguments: mainArguments);
        channel.QueueDeclare(QueueNames.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false);

        channel.QueueBind(QueueNames.MainQueue, QueueNames.Exchange, QueueNames.MainRoutingKey);
        channel.QueueBind(QueueNames.DeadLetterQueue, QueueNames.DeadLetterExchange, QueueNames.DeadLetterRoutingKey);
    }

    public Task PublishAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken = default)
    {
        using var channel = _connection.CreateModel();
        var json = JsonSerializer.Serialize(envelope);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;
        properties.ContentType = "application/json";
        properties.CorrelationId = envelope.CorrelationId;

        var routingKey = string.IsNullOrWhiteSpace(envelope.Channel) ? "telemetry.default" : $"telemetry.{envelope.Channel}";

        channel.BasicPublish(
            exchange: "telemetry.exchange",
            routingKey: "telemetry.line-1",
            basicProperties: null,
            body: body);

        _logger.LogInformation("Published message to RabbitMQ. channel={Channel} correlationId={CorrelationId}", envelope.Channel, envelope.CorrelationId);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}

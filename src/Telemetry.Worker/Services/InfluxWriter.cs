 using InfluxDB.Client;
using InfluxDB.Client.Writes;
using Telemetry.Contracts;
using InfluxDB.Client.Api.Domain;

namespace Telemetry.Worker.Services;

public sealed class InfluxWriter : IDisposable
{
    private readonly InfluxDBClient _client;
    private readonly string _org;
    private readonly string _bucket;
    private readonly ILogger<InfluxWriter> _logger;

    public InfluxWriter(IConfiguration configuration, ILogger<InfluxWriter> logger)
    {
        _logger = logger;
        var section = configuration.GetSection("InfluxDb");
        var url = section["Url"] ?? throw new InvalidOperationException("InfluxDb:Url is missing");
        var token = section["Token"] ?? throw new InvalidOperationException("InfluxDb:Token is missing");
        _org = section["Org"] ?? throw new InvalidOperationException("InfluxDb:Org is missing");
        _bucket = section["Bucket"] ?? throw new InvalidOperationException("InfluxDb:Bucket is missing");

        _client = InfluxDBClientFactory.Create(url, token.ToCharArray());
    }

    public async Task WriteAsync(TelemetryEnvelope envelope, InfluxPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            var timestamp = DateTime.UtcNow;
            var point = PointData
                .Measurement(payload.Measurement)
                .Tag("location", payload.Location)
                .Tag("channel", envelope.Channel)
                .Field("value", payload.Value)
                .Field("correlationId", envelope.CorrelationId)
                .Timestamp(timestamp, WritePrecision.Ns);

            await _client.GetWriteApiAsync()
                .WritePointAsync(point, _bucket, _org, cancellationToken);

            _logger.LogInformation("Written to InfluxDB measurement={Measurement} location={Location} value={Value}", payload.Measurement, payload.Location, payload.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Influx write failed");
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

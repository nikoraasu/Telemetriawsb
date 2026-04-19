namespace Telemetry.Contracts;

public static class QueueNames
{
    public const string Exchange = "telemetry.exchange";
    public const string DeadLetterExchange = "telemetry.dlx";
    public const string MainQueue = "telemetry.main";
    public const string DeadLetterQueue = "telemetry.dead";
    public const string MainRoutingKey = "telemetry.*";
    public const string DeadLetterRoutingKey = "telemetry.dead";
}

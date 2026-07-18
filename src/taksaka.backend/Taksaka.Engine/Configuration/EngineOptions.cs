namespace Taksaka.Engine.Configuration;

public sealed class EngineOptions
{
    public const string SectionName = "Engine";

    public int DispatchPollIntervalSeconds { get; set; } = 2;

    public int HealthMonitorIntervalSeconds { get; set; } = 60;
}

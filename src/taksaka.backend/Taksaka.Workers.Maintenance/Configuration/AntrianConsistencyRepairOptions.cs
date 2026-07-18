namespace Taksaka.Workers.Maintenance.Configuration;

internal sealed class AntrianConsistencyRepairOptions
{
    public int BatchSize { get; set; } = 50;

    public bool WorkerEnabled { get; set; } = true;

    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(10);

    public string ConnectionString { get; set; } = string.Empty;
}

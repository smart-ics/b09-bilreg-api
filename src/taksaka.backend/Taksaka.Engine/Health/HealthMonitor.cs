using Taksaka.Abstractions;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Engine.Health;

public sealed class HealthMonitor : IHealthMonitor
{
    public Task<HealthSnapshot> EvaluateAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new HealthSnapshot
        {
            Dimension = "Platform",
            State = HealthState.Healthy,
            EvaluatedAt = DateTimeOffset.UtcNow
        });
}

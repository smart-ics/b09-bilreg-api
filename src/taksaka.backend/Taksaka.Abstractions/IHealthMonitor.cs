using Taksaka.Core.Entities;

namespace Taksaka.Abstractions;

public interface IHealthMonitor
{
    Task<HealthSnapshot> EvaluateAsync(CancellationToken cancellationToken = default);
}

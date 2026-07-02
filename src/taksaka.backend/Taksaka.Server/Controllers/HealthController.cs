using Microsoft.AspNetCore.Mvc;
using Taksaka.Abstractions;
using Taksaka.Core.Enums;

namespace Taksaka.Server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthMonitor _healthMonitor;

    public HealthController(IHealthMonitor healthMonitor)
    {
        _healthMonitor = healthMonitor;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _healthMonitor.EvaluateAsync(cancellationToken);

        return Ok(new
        {
            status = snapshot.State.ToString(),
            dimension = snapshot.Dimension,
            evaluatedAt = snapshot.EvaluatedAt,
            platform = HealthState.Healthy.ToString()
        });
    }
}

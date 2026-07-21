using Microsoft.AspNetCore.Mvc;
using Taksaka.Abstractions;
using Taksaka.Core.Enums;

namespace Taksaka.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(IHealthMonitor healthMonitor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var snapshot = await healthMonitor.EvaluateAsync(cancellationToken);

        return Ok(new
        {
            status = snapshot.State.ToString(),
            dimension = snapshot.Dimension,
            evaluatedAt = snapshot.EvaluatedAt,
            registeredWorkerCount = snapshot.RegisteredWorkerCount,
            queueDepth = snapshot.QueueDepth,
            issues = snapshot.Issues
        });
    }
}

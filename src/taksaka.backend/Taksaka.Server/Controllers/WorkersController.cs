using Microsoft.AspNetCore.Mvc;
using Taksaka.Abstractions;

namespace Taksaka.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkersController(IWorkerRegistry workerRegistry) : ControllerBase
{
    [HttpGet]
    public IActionResult List()
    {
        var workers = workerRegistry.RegisteredWorkerNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name =>
            {
                workerRegistry.TryGetWorker(name, out var worker);
                return new
                {
                    name,
                    category = worker?.Descriptor.Category.ToString(),
                    maxConcurrency = worker?.Descriptor.Policy.MaxConcurrency,
                    maxRetryCount = worker?.Descriptor.Policy.MaxRetryCount
                };
            });

        return Ok(new
        {
            count = workerRegistry.RegisteredWorkerCount,
            workers
        });
    }
}

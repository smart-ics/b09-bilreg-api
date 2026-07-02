using Microsoft.AspNetCore.Mvc;
using Taksaka.Abstractions;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController(
    IQueue queue,
    IJobRepository jobRepository,
    IExecutionHistoryRepository executionHistoryRepository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WorkerName))
        {
            return BadRequest(new
            {
                error = "WorkerName is required.",
                suggestion = "Specify the worker name from the plugin manifest (e.g. 'sample-projection')."
            });
        }

        var job = new Job
        {
            Payload = request.Payload ?? string.Empty,
            WorkerName = request.WorkerName.Trim(),
            Priority = request.Priority ?? JobPriority.Normal
        };

        await queue.EnqueueAsync(job, cancellationToken);

        return Created($"/api/jobs/{job.Id}", new
        {
            job.Id,
            job.WorkerName,
            job.Status,
            job.Priority,
            job.CreatedAt
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return NotFound(new { error = $"Job '{id}' not found." });
        }

        var history = await executionHistoryRepository.GetByJobIdAsync(id, cancellationToken);

        return Ok(new
        {
            job.Id,
            job.WorkerName,
            job.Payload,
            job.Status,
            job.Priority,
            job.RetryCount,
            job.CreatedAt,
            job.EnqueuedAt,
            job.NextRetryAt,
            job.DeadLetterReason,
            history = history.Select(entry => new
            {
                entry.Id,
                entry.StartedAt,
                entry.CompletedAt,
                entry.Outcome
            })
        });
    }
}

public sealed class CreateJobRequest
{
    public string? Payload { get; init; }

    public string? WorkerName { get; init; }

    public JobPriority? Priority { get; init; }
}

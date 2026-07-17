using Taksaka.Abstractions;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Core.Policies;

namespace Taksaka.Workers.Projection.Workers;

public sealed class SampleProjectionWorker : IWorker
{
    public WorkerDescriptor Descriptor { get; } = new()
    {
        Name = "sample-projection",
        Category = WorkerCategory.Projection,
        Policy = new WorkerExecutionPolicy
        {
            MaxConcurrency = 4,
            MaxRetryCount = null,
            Timeout = null
        }
    };

    public Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default) =>
        Task.FromResult(WorkerResult.Success("Sample projection worker completed."));
}

using Taksaka.Abstractions;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Core.Policies;

namespace Taksaka.Workers.Integration.Workers;

public sealed class SampleIntegrationWorker : IWorker
{
    public WorkerDescriptor Descriptor { get; } = new()
    {
        Name = "sample-integration",
        Category = WorkerCategory.Integration,
        Policy = new WorkerExecutionPolicy
        {
            MaxConcurrency = 2,
            MaxRetryCount = 5,
            Timeout = TimeSpan.FromMinutes(5)
        }
    };

    public Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default) =>
        Task.FromResult(WorkerResult.Success("Sample integration worker completed."));
}

using Taksaka.Core.Entities;

namespace Taksaka.Abstractions;

public interface IWorker
{
    WorkerDescriptor Descriptor { get; }

    Task<WorkerResult> ExecuteAsync(Job job, CancellationToken cancellationToken = default);
}

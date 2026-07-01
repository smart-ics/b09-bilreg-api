using Taksaka.Core.Entities;

namespace Taksaka.Abstractions;

public interface IDeadLetterManager
{
    Task MoveToDeadLetterAsync(Job job, string reason, CancellationToken cancellationToken = default);
}

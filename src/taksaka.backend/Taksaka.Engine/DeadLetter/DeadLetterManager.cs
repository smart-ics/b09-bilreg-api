using Taksaka.Abstractions;
using Taksaka.Core.Entities;

namespace Taksaka.Engine.DeadLetter;

public sealed class DeadLetterManager : IDeadLetterManager
{
    public Task MoveToDeadLetterAsync(Job job, string reason, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

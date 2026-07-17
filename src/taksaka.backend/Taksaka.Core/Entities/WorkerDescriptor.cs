using Taksaka.Core.Enums;
using Taksaka.Core.Policies;

namespace Taksaka.Core.Entities;

public sealed class WorkerDescriptor
{
    public string Name { get; init; } = string.Empty;

    public WorkerCategory Category { get; init; }

    public WorkerExecutionPolicy Policy { get; init; } = new();
}

using Taksaka.Core.Enums;

namespace Taksaka.Core.Events;

public sealed record JobCreatedEvent(
    Guid JobId,
    JobPriority Priority,
    DateTimeOffset CreatedAt);

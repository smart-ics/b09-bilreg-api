using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.CpoeContext.ClinicalOrderFeature;

public record OrderHistoryEntryModel(
    string OrderHistoryEntryId,
    OrderHistoryActionEnum Action,
    string ActorId,
    DateTime OccurredAt,
    string Reason,
    string BeforeValue,
    string AfterValue)
{
    private const string IdPrefix = "COH";

    internal static OrderHistoryEntryModel Create(
        OrderHistoryActionEnum action,
        string actorId,
        DateTime occurredAt,
        string? reason,
        string? beforeValue,
        string? afterValue)
    {
        if (!Enum.IsDefined(action))
            throw new ArgumentOutOfRangeException(nameof(action));
        Guard.Against.NullOrWhiteSpace(actorId);
        if (occurredAt == default)
            throw new ArgumentException("History time wajib diisi.", nameof(occurredAt));

        return new OrderHistoryEntryModel(
            NunaId.New(IdPrefix), action, actorId, occurredAt, reason ?? string.Empty,
            beforeValue ?? string.Empty, afterValue ?? string.Empty);
    }
}

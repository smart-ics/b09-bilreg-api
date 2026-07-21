using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.BedOperationalFeature;

public record BedBlockerStateType
{
    public BedBlockerStateType(
        bool isBlocked,
        BedRestrictionTypeEnum restrictionType,
        string reason,
        string originatingTransactionId)
    {
        IsBlocked = isBlocked;
        RestrictionType = restrictionType;
        Reason = reason;
        OriginatingTransactionId = originatingTransactionId;
    }

    public bool IsBlocked { get; init; }
    public BedRestrictionTypeEnum RestrictionType { get; init; }
    public string Reason { get; init; }
    public string OriginatingTransactionId { get; init; }

    public static BedBlockerStateType None => new(
        false,
        BedRestrictionTypeEnum.None,
        string.Empty,
        string.Empty);

    internal static BedBlockerStateType From(BedReadinessTransactionModel transaction)
    {
        Guard.Against.Null(transaction);

        return transaction.NewStatus == BedReadinessStatusEnum.Ready
            ? None
            : new BedBlockerStateType(
                true,
                transaction.RestrictionType,
                transaction.Reason,
                transaction.TransactionId);
    }
}

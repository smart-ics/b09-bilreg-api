using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

public class StockLegacyScopeModel : IStockLegacyScopeKey
{
    #region CREATION

    public StockLegacyScopeModel(
        string brgId,
        string brgMasukReffId,
        AlignmentStatusEnum alignmentStatus,
        DateTime tglMutasiLast,
        string lastLegacyBukuId,
        DateTime lastSyncedAt,
        string inconsistencyReason)
    {
        BrgId = brgId;
        BrgMasukReffId = brgMasukReffId;
        AlignmentStatus = alignmentStatus;
        TglMutasiLast = tglMutasiLast;
        LastLegacyBukuId = lastLegacyBukuId;
        LastSyncedAt = lastSyncedAt;
        InconsistencyReason = inconsistencyReason;
    }

    public static StockLegacyScopeModel CreateNotAligned(string brgId, string brgMasukReffId)
    {
        Guard.Against.NullOrWhiteSpace(brgId);
        Guard.Against.NullOrWhiteSpace(brgMasukReffId);

        return new StockLegacyScopeModel(
            brgId,
            brgMasukReffId,
            AlignmentStatusEnum.NotAligned,
            StockLedgerSentinel.EmptyDate,
            lastLegacyBukuId: string.Empty,
            StockLedgerSentinel.EmptyDate,
            inconsistencyReason: string.Empty);
    }

    public static StockLegacyScopeModel Default =>
        new("-", "-", AlignmentStatusEnum.NotAligned, StockLedgerSentinel.EmptyDate,
            string.Empty, StockLedgerSentinel.EmptyDate, string.Empty);

    public static IStockLegacyScopeKey Key(string brgId, string brgMasukReffId) =>
        new StockLegacyScopeModel(brgId, brgMasukReffId, AlignmentStatusEnum.NotAligned,
            StockLedgerSentinel.EmptyDate, string.Empty, StockLedgerSentinel.EmptyDate, string.Empty);

    #endregion

    #region PROPERTIES

    public string BrgId { get; init; }
    public string BrgMasukReffId { get; init; }
    public AlignmentStatusEnum AlignmentStatus { get; private set; }
    public DateTime TglMutasiLast { get; private set; }
    public string LastLegacyBukuId { get; private set; }
    public DateTime LastSyncedAt { get; private set; }
    public string InconsistencyReason { get; private set; }

    #endregion

    #region BEHAVIOUR

    public void MarkAligned(DateTime tglMutasiLast, string lastLegacyBukuId, DateTime lastSyncedAt)
    {
        AlignmentStatus = AlignmentStatusEnum.Aligned;
        TglMutasiLast = tglMutasiLast;
        LastLegacyBukuId = lastLegacyBukuId ?? string.Empty;
        LastSyncedAt = lastSyncedAt;
        InconsistencyReason = string.Empty;
    }

    public void MarkStale()
    {
        AlignmentStatus = AlignmentStatusEnum.Stale;
    }

    public void MarkInconsistent(string reason)
    {
        Guard.Against.NullOrWhiteSpace(reason);
        AlignmentStatus = AlignmentStatusEnum.Inconsistent;
        InconsistencyReason = reason;
    }

    public void AdvanceWatermark(DateTime tglMutasiLast, string lastLegacyBukuId, DateTime lastSyncedAt)
    {
        TglMutasiLast = tglMutasiLast;
        LastLegacyBukuId = lastLegacyBukuId ?? string.Empty;
        LastSyncedAt = lastSyncedAt;
        if (AlignmentStatus == AlignmentStatusEnum.Stale)
            AlignmentStatus = AlignmentStatusEnum.Aligned;
    }

    #endregion
}

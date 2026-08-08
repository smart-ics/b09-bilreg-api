using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S2 — Caller-supplied in-memory Ledger snapshot for sync delta interpretation.
/// Loaded by P3-S4 orchestration before calling <see cref="LegacySyncDeltaInterpreter"/>;
/// the interpreter never performs I/O.
/// </summary>
public sealed record LegacySyncLedgerSnapshot(
    IReadOnlyDictionary<LegacyJournalIdentity, LegacySyncJournalAnchor> JournalAnchors,
    IReadOnlyDictionary<LegacyBalanceIdentity, LegacySyncBalanceAnchor> BalanceAnchors,
    IReadOnlyList<StockMovementModel> Movements)
{
    public static LegacySyncLedgerSnapshot Empty { get; } = new(
        new Dictionary<LegacyJournalIdentity, LegacySyncJournalAnchor>(),
        new Dictionary<LegacyBalanceIdentity, LegacySyncBalanceAnchor>(),
        Array.Empty<StockMovementModel>());

    public static LegacySyncLedgerSnapshot Create(
        IReadOnlyDictionary<LegacyJournalIdentity, LegacySyncJournalAnchor>? journalAnchors = null,
        IReadOnlyDictionary<LegacyBalanceIdentity, LegacySyncBalanceAnchor>? balanceAnchors = null,
        IReadOnlyList<StockMovementModel>? movements = null)
        => new(
            journalAnchors ?? new Dictionary<LegacyJournalIdentity, LegacySyncJournalAnchor>(),
            balanceAnchors ?? new Dictionary<LegacyBalanceIdentity, LegacySyncBalanceAnchor>(),
            movements ?? Array.Empty<StockMovementModel>());
}

/// <summary>
/// Links a Ledger-known legacy journal identity to the Stock Movement that previously
/// incorporated it (for Reverse / Correct targets).
/// </summary>
public sealed record LegacySyncJournalAnchor(
    string StockMovementId,
    StockMovementModel? Movement = null);

/// <summary>
/// Links a Ledger-known legacy balance identity to a Stock Layer.
/// Layer <see cref="LayerOrigin"/> is preserved on quantity-only sync paths (BR-STL-113).
/// </summary>
public sealed record LegacySyncBalanceAnchor(
    string StockLayerId,
    decimal RemainingQuantity,
    StockFactOriginEnum LayerOrigin,
    decimal UnitCost,
    DateOnly? ExpirationDate = null,
    string? Batch = null);

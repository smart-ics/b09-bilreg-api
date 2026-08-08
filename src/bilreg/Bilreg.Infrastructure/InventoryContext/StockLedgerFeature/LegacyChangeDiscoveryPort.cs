using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S1 / G-13 — Live deletion-aware Legacy Change Discovery adapter.
/// Read-only: legacy snapshot via <see cref="ILegacyStockReadPort"/>, Ledger-known identities via idempotency keys,
/// fingerprint via <see cref="LegacyReconstructionBasisCalculator"/> only.
/// Does not advance Synchronization Position, mutate legacy tables, or apply sync catch-up.
/// </summary>
public sealed class LegacyChangeDiscoveryPort : ILegacyChangeDiscoveryPort
{
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly IStockSourceIdempotencyDal _idempotencyDal;

    public LegacyChangeDiscoveryPort(
        ILegacyStockReadPort legacyStockReadPort,
        IStockSourceIdempotencyDal idempotencyDal)
    {
        _legacyStockReadPort = legacyStockReadPort;
        _idempotencyDal = idempotencyDal;
    }

    public SynchronizationPositionType ComputeCurrentFingerprint(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var balances = _legacyStockReadPort.ListCurrentBalances(scope);
        var journals = _legacyStockReadPort.ListJournalEntries(scope);
        return LegacyReconstructionBasisCalculator.Compute(balances, journals);
    }

    public LegacyChangeDiscoveryResult DiscoverChanges(
        IStockLedgerScopeKey scope,
        SynchronizationPositionType? storedPosition)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var balances = _legacyStockReadPort.ListCurrentBalances(scope);
        var journals = _legacyStockReadPort.ListJournalEntries(scope);
        var currentFingerprint = LegacyReconstructionBasisCalculator.Compute(balances, journals);
        var identityKeys = _idempotencyDal.ListSyncIdentityKeysForScope(scope.BrgId, scope.ReceiptSourceId);
        var known = LegacyChangeDiscoveryClassifier.ParseKnownIdentities(identityKeys);

        return LegacyChangeDiscoveryClassifier.Classify(
            scope,
            storedPosition,
            currentFingerprint,
            balances,
            journals,
            known);
    }
}

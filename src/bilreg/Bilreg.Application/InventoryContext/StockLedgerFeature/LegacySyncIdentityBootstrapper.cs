using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S4 / R-001 — Establishes the complete Ledger-known discovery identity set
/// (<c>SYNC|BUKU|…</c> / <c>SYNC|STOK|…</c>) as quantity-neutral SyncBatch evidence.
/// </summary>
public sealed class LegacySyncIdentityBootstrapper
{
    private readonly IStockConsequenceUnitOfWork _unitOfWork;
    private readonly IStockSourceIdempotencyRepo _idempotencyRepo;
    private readonly IStockMovementRepo _movementRepo;

    public LegacySyncIdentityBootstrapper(
        IStockConsequenceUnitOfWork unitOfWork,
        IStockSourceIdempotencyRepo idempotencyRepo,
        IStockMovementRepo movementRepo)
    {
        _unitOfWork = unitOfWork;
        _idempotencyRepo = idempotencyRepo;
        _movementRepo = movementRepo;
    }

    /// <summary>
    /// True when every surviving journal/balance already has a parseable material discovery key.
    /// </summary>
    public static bool HasCompleteCoverage(
        IStockLedgerScopeKey scope,
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        IReadOnlyList<LegacyStockBalanceType> balances,
        IEnumerable<string> existingIdentityKeys)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(journals);
        ArgumentNullException.ThrowIfNull(balances);
        ArgumentNullException.ThrowIfNull(existingIdentityKeys);

        var known = new HashSet<string>(existingIdentityKeys, StringComparer.Ordinal);
        foreach (var journal in journals)
        {
            if (!known.Contains(LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(scope, journal)))
                return false;
        }

        foreach (var balance in balances.Where(b => !string.IsNullOrWhiteSpace(b.LegacyRowId)))
        {
            if (!known.Contains(LegacyChangeDiscoveryIdentityKeys.BuildBalanceKey(scope, balance)))
                return false;
        }

        return true;
    }

    public int Bootstrap(
        IStockLedgerScopeKey scope,
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        IReadOnlyList<LegacyStockBalanceType> balances,
        DateTime processedAt)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(journals);
        ArgumentNullException.ThrowIfNull(balances);
        if (processedAt == default)
            throw new ArgumentException("ProcessedAt is required.", nameof(processedAt));

        var reconstructionMovementId = LegacyReconstructionBaselineCalculator.BuildMovementId(scope);
        var hasReconstructionMovement = _movementRepo
            .LoadEntity(StockMovementModel.Key(reconstructionMovementId))
            .HasValue;

        var inserted = 0;
        foreach (var journal in journals)
        {
            var key = LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(scope, journal);
            var result = _unitOfWork.CommitSyncEvidence(new StockSyncEvidenceDraft(
                IdempotencyKey: key,
                ProcessedAt: processedAt,
                BrgId: scope.BrgId,
                ReceiptSourceId: scope.ReceiptSourceId,
                StockMovementId: hasReconstructionMovement ? reconstructionMovementId : null,
                SourceTransactionId: journal.MutationTransactionId));

            if (result.Outcome == StockConsequenceCommitOutcomeEnum.Committed)
                inserted++;
        }

        foreach (var balance in balances.Where(b => !string.IsNullOrWhiteSpace(b.LegacyRowId)))
        {
            var key = LegacyChangeDiscoveryIdentityKeys.BuildBalanceKey(scope, balance);
            var result = _unitOfWork.CommitSyncEvidence(new StockSyncEvidenceDraft(
                IdempotencyKey: key,
                ProcessedAt: processedAt,
                BrgId: scope.BrgId,
                ReceiptSourceId: scope.ReceiptSourceId,
                StockMovementId: hasReconstructionMovement ? reconstructionMovementId : null,
                SourceTransactionId: balance.LegacyRowId));

            if (result.Outcome == StockConsequenceCommitOutcomeEnum.Committed)
                inserted++;
        }

        return inserted;
    }

    public bool HasCompleteCoverage(IStockLedgerScopeKey scope,
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        IReadOnlyList<LegacyStockBalanceType> balances)
    {
        var keys = _idempotencyRepo.ListSyncIdentityRecordsForScope(scope)
            .Select(r => r.IdempotencyKey);
        return HasCompleteCoverage(scope, journals, balances, keys);
    }
}

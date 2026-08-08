using Ardalis.GuardClauses;
using Bilreg.Application.Shared;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S5 — Phase A reconstruction claim for one Item + Receipt Source.
/// Short transaction only: create Scope if missing, transition to
/// <see cref="ReconstructionStatusEnum.Reconstructing"/>, commit.
/// Does not read legacy history, calculate baselines, fingerprint, or persist Movements/Layers.
/// </summary>
public sealed class ReconstructionClaimService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;

    public ReconstructionClaimService(
        IUnitOfWork unitOfWork,
        IStockLedgerScopeStateRepo scopeStateRepo)
    {
        _unitOfWork = unitOfWork;
        _scopeStateRepo = scopeStateRepo;
    }

    /// <summary>
    /// Acquires exactly one active reconstruction claim for the scope.
    /// Concurrent losers fail closed (<see cref="ReconstructionClaimOutcomeEnum.AlreadyClaimed"/>)
    /// without corrupting durable state. Illegal source statuses throw
    /// <see cref="InvalidOperationException"/> via domain transitions.
    /// </summary>
    public ReconstructionClaimResult Claim(IStockLedgerScopeKey scope)
    {
        Guard.Against.Null(scope, nameof(scope));
        Guard.Against.NullOrWhiteSpace(scope.BrgId, nameof(scope.BrgId));
        Guard.Against.NullOrWhiteSpace(scope.ReceiptSourceId, nameof(scope.ReceiptSourceId));

        var key = StockLedgerScopeKeyType.Create(scope.BrgId, scope.ReceiptSourceId);

        using var tx = _unitOfWork.Begin();

        var loaded = _scopeStateRepo.LoadEntity(key);
        if (!loaded.HasValue)
        {
            var inserted = BuildClaimedFromScratch(key);
            if (_scopeStateRepo.TryInsertNew(inserted))
            {
                tx.Complete();
                return ReconstructionClaimResult.Claimed(inserted);
            }

            // Concurrent insert won the PK — continue against the durable winner.
            loaded = _scopeStateRepo.LoadEntity(key);
            if (!loaded.HasValue)
            {
                throw StockLedgerPersistenceException.Integrity(
                    $"Scope ({key.BrgId}/{key.ReceiptSourceId}) insert raced but the " +
                    "existing row could not be loaded.");
            }
        }

        var current = loaded.Value;
        if (current.ReconstructionStatus == ReconstructionStatusEnum.Reconstructing)
            return ReconstructionClaimResult.AlreadyClaimed(current);

        var expectedPrior = current.ReconstructionStatus;
        var next = ApplyClaimTransitions(current);

        if (!_scopeStateRepo.TryUpdateWhenReconstructionStatus(next, expectedPrior))
        {
            var afterRace = _scopeStateRepo.LoadEntity(key);
            if (!afterRace.HasValue)
            {
                throw StockLedgerPersistenceException.Integrity(
                    $"Scope ({key.BrgId}/{key.ReceiptSourceId}) claim update raced but the " +
                    "row could not be reloaded.");
            }

            return ReconstructionClaimResult.AlreadyClaimed(afterRace.Value);
        }

        tx.Complete();
        return ReconstructionClaimResult.Claimed(next);
    }

    private static StockLedgerScopeStateModel BuildClaimedFromScratch(IStockLedgerScopeKey key)
        => StockLedgerScopeStateModel
            .CreateNotReconstructed(key)
            .RequireReconstruction()
            .BeginReconstruction();

    private static StockLedgerScopeStateModel ApplyClaimTransitions(StockLedgerScopeStateModel current)
        => current.ReconstructionStatus switch
        {
            ReconstructionStatusEnum.NotReconstructed
                => current.RequireReconstruction().BeginReconstruction(),
            ReconstructionStatusEnum.ReconstructionRequired
                => current.BeginReconstruction(),
            // Reconstructed / Inconsistent / unexpected: domain rejects BeginReconstruction.
            _
                => current.BeginReconstruction()
        };
}

/// <summary>Outcome of a Phase A reconstruction claim attempt.</summary>
public enum ReconstructionClaimOutcomeEnum
{
    /// <summary>This caller successfully acquired Reconstructing.</summary>
    Claimed = 1,

    /// <summary>
    /// Another claimant already holds Reconstructing, or this caller lost the
    /// conditional-update race. Durable state is not corrupted.
    /// </summary>
    AlreadyClaimed = 2
}

public sealed record ReconstructionClaimResult(
    ReconstructionClaimOutcomeEnum Outcome,
    StockLedgerScopeStateModel ScopeState)
{
    public static ReconstructionClaimResult Claimed(StockLedgerScopeStateModel scopeState)
        => new(ReconstructionClaimOutcomeEnum.Claimed, scopeState);

    public static ReconstructionClaimResult AlreadyClaimed(StockLedgerScopeStateModel scopeState)
        => new(ReconstructionClaimOutcomeEnum.AlreadyClaimed, scopeState);
}

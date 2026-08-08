using Ardalis.GuardClauses;
using Bilreg.Application.Shared;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S6 — .NET-side synchronization claim for one Item + Receipt Source.
/// Short transaction only: transition Scope to
/// <see cref="SynchronizationStateEnum.SynchronizationRequired"/> (or resume an existing claim).
/// Does not discover, interpret, persist movements, or advance Synchronization Position.
/// </summary>
public sealed class SynchronizationClaimService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockLedgerScopeStateRepo _scopeStateRepo;

    public SynchronizationClaimService(
        IUnitOfWork unitOfWork,
        IStockLedgerScopeStateRepo scopeStateRepo)
    {
        _unitOfWork = unitOfWork;
        _scopeStateRepo = scopeStateRepo;
    }

    /// <summary>
    /// Acquires a synchronization claim for the scope.
    /// When <paramref name="allowResume"/> is false, an existing
    /// <see cref="SynchronizationStateEnum.SynchronizationRequired"/> fails closed as
    /// <see cref="SynchronizationClaimOutcomeEnum.AlreadyClaimed"/> (concurrent serialization).
    /// When <paramref name="allowResume"/> is true, an existing SynchronizationRequired is
    /// treated as a crash-resume claim (bounded retry / recovery path).
    /// </summary>
    public SynchronizationClaimResult Claim(IStockLedgerScopeKey scope, bool allowResume)
    {
        Guard.Against.Null(scope, nameof(scope));
        Guard.Against.NullOrWhiteSpace(scope.BrgId, nameof(scope.BrgId));
        Guard.Against.NullOrWhiteSpace(scope.ReceiptSourceId, nameof(scope.ReceiptSourceId));

        var key = StockLedgerScopeKeyType.Create(scope.BrgId, scope.ReceiptSourceId);
        var loaded = _scopeStateRepo.LoadEntity(key);
        if (!loaded.HasValue)
        {
            throw StockLedgerPersistenceException.Integrity(
                $"Scope ({key.BrgId}/{key.ReceiptSourceId}) was not found.");
        }

        var current = loaded.Value;

        if (current.SynchronizationState == SynchronizationStateEnum.SynchronizationRequired)
        {
            return allowResume
                ? SynchronizationClaimResult.Claimed(current)
                : SynchronizationClaimResult.AlreadyClaimed(current);
        }

        if (current.SynchronizationState is not (
                SynchronizationStateEnum.Current
                or SynchronizationStateEnum.LegacyChangePending))
        {
            return SynchronizationClaimResult.AlreadyClaimed(current);
        }

        var prior = current.SynchronizationState;
        var next = current.RequireSynchronization();

        using var tx = _unitOfWork.Begin();
        if (!_scopeStateRepo.TryUpdateWhenSynchronizationState(next, prior))
        {
            var afterRace = _scopeStateRepo.LoadEntity(key);
            if (!afterRace.HasValue)
            {
                throw StockLedgerPersistenceException.Integrity(
                    $"Scope ({key.BrgId}/{key.ReceiptSourceId}) claim update raced but the " +
                    "row could not be reloaded.");
            }

            return SynchronizationClaimResult.AlreadyClaimed(afterRace.Value);
        }

        tx.Complete();
        return SynchronizationClaimResult.Claimed(next);
    }
}

/// <summary>Outcome of a synchronization claim attempt.</summary>
public enum SynchronizationClaimOutcomeEnum
{
    /// <summary>This caller successfully acquired or resumed SynchronizationRequired.</summary>
    Claimed = 1,

    /// <summary>
    /// Another claimant already holds SynchronizationRequired, or this caller lost the
    /// conditional-update race. Durable state is not corrupted.
    /// </summary>
    AlreadyClaimed = 2
}

public sealed record SynchronizationClaimResult(
    SynchronizationClaimOutcomeEnum Outcome,
    StockLedgerScopeStateModel ScopeState)
{
    public static SynchronizationClaimResult Claimed(StockLedgerScopeStateModel scopeState)
        => new(SynchronizationClaimOutcomeEnum.Claimed, scopeState);

    public static SynchronizationClaimResult AlreadyClaimed(StockLedgerScopeStateModel scopeState)
        => new(SynchronizationClaimOutcomeEnum.AlreadyClaimed, scopeState);
}

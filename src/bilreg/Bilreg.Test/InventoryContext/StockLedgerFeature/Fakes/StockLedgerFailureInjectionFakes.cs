using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;

/// <summary>
/// P4-S3 — Test-only failure-injection decorators for live G-18 atomicity proofs.
/// Not for production DI.
/// </summary>
public sealed class ThrowingStockMovementRepo : IStockMovementRepo
{
    private readonly IStockMovementRepo _inner;
    private readonly string _message;

    public ThrowingStockMovementRepo(
        IStockMovementRepo inner,
        string message = "Forced movement persist failure for P4-S3 rollback test.")
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _message = message;
    }

    public void SaveChanges(StockMovementModel model)
        => throw new InvalidOperationException(_message);

    public MayBe<StockMovementModel> LoadEntity(IStockMovementKey key)
        => _inner.LoadEntity(key);
}

/// <summary>
/// P4-S3 / P2-S6 — throws on SaveChanges; delegates reads.
/// </summary>
public sealed class ThrowingStockPositionRepo : IStockPositionRepo
{
    private readonly IStockPositionRepo _inner;
    private readonly string _message;

    public ThrowingStockPositionRepo(
        IStockPositionRepo inner,
        string message = "Forced position persist failure for P4-S3 rollback test.")
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _message = message;
    }

    public void SaveChanges(StockPositionModel model)
        => throw new InvalidOperationException(_message);

    public MayBe<StockPositionModel> LoadEntity(IStockWriteScopeKey key)
        => _inner.LoadEntity(key);

    public IReadOnlyList<StockPositionModel> ListByLedgerScope(IStockLedgerScopeKey scope)
        => _inner.ListByLedgerScope(scope);
}

/// <summary>
/// P4-S3 — throws on SaveChanges; delegates reads and conditional updates.
/// </summary>
public sealed class ThrowingStockLedgerScopeStateRepo : IStockLedgerScopeStateRepo
{
    private readonly IStockLedgerScopeStateRepo _inner;
    private readonly string _message;

    public ThrowingStockLedgerScopeStateRepo(
        IStockLedgerScopeStateRepo inner,
        string message = "Forced scope persist failure for P4-S3 rollback test.")
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _message = message;
    }

    public void SaveChanges(StockLedgerScopeStateModel model)
        => throw new InvalidOperationException(_message);

    public MayBe<StockLedgerScopeStateModel> LoadEntity(IStockLedgerScopeKey key)
        => _inner.LoadEntity(key);

    public bool TryInsertNew(StockLedgerScopeStateModel model)
        => _inner.TryInsertNew(model);

    public bool TryUpdateWhenReconstructionStatus(
        StockLedgerScopeStateModel model,
        ReconstructionStatusEnum expectedPriorStatus)
        => _inner.TryUpdateWhenReconstructionStatus(model, expectedPriorStatus);

    public bool TryUpdateWhenSynchronizationState(
        StockLedgerScopeStateModel model,
        SynchronizationStateEnum expectedPriorState)
        => _inner.TryUpdateWhenSynchronizationState(model, expectedPriorState);
}

/// <summary>
/// P4-S3 — wraps an inner writer and throws on Apply (after optional callback).
/// Use with live <see cref="Bilreg.Infrastructure.InventoryContext.StockLedgerFeature.LegacyCompatibilityWriterPort"/>
/// to prove Ledger rollback when legacy Apply fails before Complete.
/// </summary>
public sealed class ThrowingLegacyCompatibilityWriterPort : ILegacyCompatibilityWriterPort
{
    private readonly ILegacyCompatibilityWriterPort? _inner;
    private readonly string _message;

    public ThrowingLegacyCompatibilityWriterPort(
        ILegacyCompatibilityWriterPort? inner = null,
        string message = "Forced legacy Apply failure for P4-S3 rollback test.")
    {
        _inner = inner;
        _message = message;
    }

    /// <summary>
    /// When true (default), Apply throws without calling the inner writer.
    /// When false, Apply delegates to the inner writer (if present).
    /// </summary>
    public bool ThrowOnApply { get; set; } = true;

    public Action<LegacyCompatibilityWriteRequest>? OnApply { get; set; }

    public void Apply(LegacyCompatibilityWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        OnApply?.Invoke(request);
        if (ThrowOnApply)
            throw new InvalidOperationException(_message);
        _inner?.Apply(request);
    }
}

/// <summary>
/// P4-S3 — applies the first <see cref="SucceedLineCount"/> journal/balance pairs via the
/// inner (live) writer, then throws so the ambient TX rolls back mid multi-row legacy write.
/// </summary>
public sealed class PartialLegacyCompatibilityWriterPort : ILegacyCompatibilityWriterPort
{
    private readonly ILegacyCompatibilityWriterPort _inner;
    private readonly int _succeedLineCount;
    private readonly string _message;

    public PartialLegacyCompatibilityWriterPort(
        ILegacyCompatibilityWriterPort inner,
        int succeedLineCount = 1,
        string message = "Forced mid-legacy multi-row failure for P4-S3 rollback test.")
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (succeedLineCount < 0)
            throw new ArgumentOutOfRangeException(nameof(succeedLineCount));
        _succeedLineCount = succeedLineCount;
        _message = message;
    }

    public int AppliedLineCount { get; private set; }

    public void Apply(LegacyCompatibilityWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.BalanceMutations.Count != request.JournalEntries.Count)
        {
            throw new InvalidOperationException(
                "Partial legacy writer requires BalanceMutations and JournalEntries paired 1:1.");
        }

        if (_succeedLineCount > 0 && request.BalanceMutations.Count > 0)
        {
            var take = Math.Min(_succeedLineCount, request.BalanceMutations.Count);
            var partial = request with
            {
                BalanceMutations = request.BalanceMutations.Take(take).ToArray(),
                JournalEntries = request.JournalEntries.Take(take).ToArray()
            };
            _inner.Apply(partial);
            AppliedLineCount = take;
        }

        throw new InvalidOperationException(_message);
    }
}

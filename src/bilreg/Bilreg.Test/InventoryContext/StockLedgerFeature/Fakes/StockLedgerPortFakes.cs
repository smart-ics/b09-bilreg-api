using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature.Fakes;

/// <summary>
/// Test-only doubles for P1-S7 ports. Not for production DI.
/// Compatible with P1-S8 UoW wiring (especially throw-on-Apply for rollback proof).
/// </summary>
public sealed class FakeLegacyStockReadPort : ILegacyStockReadPort
{
    public IReadOnlyList<LegacyStockBalanceType> Balances { get; set; } =
        Array.Empty<LegacyStockBalanceType>();

    public IReadOnlyList<LegacyStockJournalEntryType> JournalEntries { get; set; } =
        Array.Empty<LegacyStockJournalEntryType>();

    public List<IStockLedgerScopeKey> BalanceRequests { get; } = [];
    public List<IStockLedgerScopeKey> JournalRequests { get; } = [];

    public IReadOnlyList<LegacyStockBalanceType> ListCurrentBalances(IStockLedgerScopeKey scope)
    {
        BalanceRequests.Add(scope);
        return Balances;
    }

    public IReadOnlyList<LegacyStockJournalEntryType> ListJournalEntries(IStockLedgerScopeKey scope)
    {
        JournalRequests.Add(scope);
        return JournalEntries;
    }
}

/// <summary>
/// Configurable Legacy Compatibility Writer fake for consequence UoW rollback tests (P1-S8).
/// </summary>
public sealed class FakeLegacyCompatibilityWriterPort : ILegacyCompatibilityWriterPort
{
    public bool ThrowOnApply { get; set; }
    public Exception? ExceptionToThrow { get; set; }
    public List<LegacyCompatibilityWriteRequest> Applied { get; } = [];

    public void Apply(LegacyCompatibilityWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (ThrowOnApply)
            throw ExceptionToThrow ?? new InvalidOperationException("Fake legacy compatibility writer failure.");
        Applied.Add(request);
    }
}

public sealed class FakeLegacyChangeDiscoveryPort : ILegacyChangeDiscoveryPort
{
    public SynchronizationPositionType? FingerprintToReturn { get; set; }
    public LegacyChangeDiscoveryResult? DiscoveryResult { get; set; }
    public bool ThrowOnDiscover { get; set; }

    public SynchronizationPositionType ComputeCurrentFingerprint(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return FingerprintToReturn
               ?? SynchronizationPositionType.CreateFromUtf8Token("fake-fingerprint", "test-v1");
    }

    public LegacyChangeDiscoveryResult DiscoverChanges(
        IStockLedgerScopeKey scope,
        SynchronizationPositionType? storedPosition)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (ThrowOnDiscover)
            throw new InvalidOperationException("Fake legacy change discovery failure.");

        return DiscoveryResult
               ?? new LegacyChangeDiscoveryResult(
                   LegacyChangeDiscoveryOutcomeEnum.Unchanged,
                   CurrentFingerprint: FingerprintToReturn
                                        ?? SynchronizationPositionType.CreateFromUtf8Token(
                                            "fake-fingerprint", "test-v1"),
                   Deltas: Array.Empty<LegacyDiscoveredDeltaType>(),
                   Explanation: "Fake: no live discovery.");
    }
}

public sealed class FakeAvailabilityDiscoveryPort : IAvailabilityDiscoveryPort
{
    public AvailabilityDiscoveryResult Result { get; set; } =
        new(
            AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock,
            Array.Empty<AvailabilityCandidateType>(),
            "Fake: no live availability discovery.");

    public AvailabilityDiscoveryResult Discover(
        IBrgKey item,
        ILayananKey stockLocation,
        DateOnly? expirationDateFilter = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(stockLocation);
        return Result;
    }
}

public sealed class FakeProvenanceDiscoveryPort : IProvenanceDiscoveryPort
{
    public ProvenanceDiscoveryResult Result { get; set; } =
        new(
            ProvenanceDiscoveryOutcomeEnum.Unknown,
            ReceiptSourceId: null,
            StockLayerId: null,
            CandidateReceiptSourceIds: Array.Empty<string>(),
            Explanation: "Fake: no live provenance discovery.");

    public ProvenanceDiscoveryResult Discover(ProvenanceDiscoveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Result;
    }
}

public sealed class FakeStockReconciliationPort : IStockReconciliationPort
{
    public StockReconciliationResult Result { get; set; } =
        new(
            StockReconciliationOutcomeEnum.Balanced,
            LegacyRemainingQuantity: 0m,
            LedgerRemainingQuantity: 0m,
            DifferenceQuantity: 0m,
            Differences: Array.Empty<StockReconciliationDifferenceType>(),
            Explanation: "Fake: no live reconciliation.");

    public StockReconciliationResult Reconcile(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return Result;
    }
}

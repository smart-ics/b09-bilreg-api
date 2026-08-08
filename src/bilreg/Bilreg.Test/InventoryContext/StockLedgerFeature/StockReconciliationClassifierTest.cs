using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S3 / G-16 P0 — Pure material reconciliation classification (no I/O).
/// </summary>
public class StockReconciliationClassifierTest
{
    private static readonly StockLedgerScopeKeyType Scope =
        StockLedgerScopeKeyType.Create("BRG01", "DO001");

    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

    [Fact]
    public void MultiLocation_MatchingQuantities_IsBalanced()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 10m),
            Balance("LY02", 7m)
        };
        var layers = new List<StockReconciliationClassifier.LedgerLayerSnapshot>
        {
            Layer("LY01", 10m, isDepleted: false),
            Layer("LY02", 7m, isDepleted: false)
        };

        var result = StockReconciliationClassifier.Classify(Scope, balances, layers);

        result.Outcome.Should().Be(StockReconciliationOutcomeEnum.Balanced);
        result.LegacyRemainingQuantity.Should().Be(17m);
        result.LedgerRemainingQuantity.Should().Be(17m);
        result.DifferenceQuantity.Should().Be(0m);
        result.Differences.Should().BeEmpty();
        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeTrue();
    }

    [Fact]
    public void DepletedLayer_AbsentLegacyRow_IsIntentionalDifference_NotMaterialInconsistency()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 5m)
        };
        var layers = new List<StockReconciliationClassifier.LedgerLayerSnapshot>
        {
            Layer("LY01", 5m, isDepleted: false),
            Layer("LY02", 0m, isDepleted: true)
        };

        var result = StockReconciliationClassifier.Classify(Scope, balances, layers);

        result.Outcome.Should().Be(StockReconciliationOutcomeEnum.IntentionalDifference);
        result.LegacyRemainingQuantity.Should().Be(5m);
        result.LedgerRemainingQuantity.Should().Be(5m);
        result.Differences.Should().ContainSingle(d =>
            d.Kind == StockReconciliationDifferenceKindEnum.DepletedLayerVsAbsentLegacyRow
            && d.LayananId == "LY02");
        result.Outcome.Should().NotBe(StockReconciliationOutcomeEnum.MaterialInconsistency);
        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeTrue();
    }

    [Fact]
    public void PerLocation_QuantityMismatch_IsMaterialInconsistency()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 10m)
        };
        var layers = new List<StockReconciliationClassifier.LedgerLayerSnapshot>
        {
            Layer("LY01", 8m, isDepleted: false)
        };

        var result = StockReconciliationClassifier.Classify(Scope, balances, layers);

        result.Outcome.Should().Be(StockReconciliationOutcomeEnum.MaterialInconsistency);
        result.LegacyRemainingQuantity.Should().Be(10m);
        result.LedgerRemainingQuantity.Should().Be(8m);
        result.DifferenceQuantity.Should().Be(-2m);
        result.Differences.Should().ContainSingle(d =>
            d.Kind == StockReconciliationDifferenceKindEnum.QuantityMismatch
            && d.LayananId == "LY01");
        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
    }

    [Fact]
    public void LedgerActiveQuantity_WithNoLegacyRow_IsMaterialInconsistency()
    {
        var balances = Array.Empty<LegacyStockBalanceType>();
        var layers = new List<StockReconciliationClassifier.LedgerLayerSnapshot>
        {
            Layer("LY01", 3m, isDepleted: false)
        };

        var result = StockReconciliationClassifier.Classify(Scope, balances, layers);

        result.Outcome.Should().Be(StockReconciliationOutcomeEnum.MaterialInconsistency);
        result.Differences.Should().ContainSingle(d =>
            d.Kind == StockReconciliationDifferenceKindEnum.QuantityMismatch
            && d.LayananId == "LY01");
        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
    }

    [Fact]
    public void LegacyQuantity_WithNoLedgerLayers_IsMaterialInconsistency()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 4m)
        };
        var layers = Array.Empty<StockReconciliationClassifier.LedgerLayerSnapshot>();

        var result = StockReconciliationClassifier.Classify(Scope, balances, layers);

        result.Outcome.Should().Be(StockReconciliationOutcomeEnum.MaterialInconsistency);
        result.Differences.Should().ContainSingle(d =>
            d.Kind == StockReconciliationDifferenceKindEnum.MissingLedgerMovement
            && d.LayananId == "LY01");
        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
    }

    [Fact]
    public void ScopeTotalMismatch_AcrossLocations_IsMaterialInconsistency()
    {
        // Per-location equal at LY01, but LY02 has ledger-only active qty → totals diverge.
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 5m)
        };
        var layers = new List<StockReconciliationClassifier.LedgerLayerSnapshot>
        {
            Layer("LY01", 5m, isDepleted: false),
            Layer("LY02", 2m, isDepleted: false)
        };

        var result = StockReconciliationClassifier.Classify(Scope, balances, layers);

        result.Outcome.Should().Be(StockReconciliationOutcomeEnum.MaterialInconsistency);
        result.LegacyRemainingQuantity.Should().Be(5m);
        result.LedgerRemainingQuantity.Should().Be(7m);
        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
    }

    [Fact]
    public void PendingSynchronization_IsMaterialSafeToAdvance()
    {
        var result = new StockReconciliationResult(
            StockReconciliationOutcomeEnum.PendingSynchronization,
            LegacyRemainingQuantity: 5m,
            LedgerRemainingQuantity: 5m,
            DifferenceQuantity: 0m,
            Differences: Array.Empty<StockReconciliationDifferenceType>(),
            Explanation: "Scope Synchronization State is SynchronizationRequired; material quantities match.");

        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeTrue();
    }

    [Fact]
    public void ProvenanceLimitation_IsNotMaterialSafeToAdvance()
    {
        var result = new StockReconciliationResult(
            StockReconciliationOutcomeEnum.ProvenanceLimitation,
            LegacyRemainingQuantity: null,
            LedgerRemainingQuantity: null,
            DifferenceQuantity: null,
            Differences: Array.Empty<StockReconciliationDifferenceType>(),
            Explanation: "Scope has no reconstructed baseline.");

        StockReconciliationClassifier.AllowsMaterialSynchronizationAdvance(result).Should().BeFalse();
    }

    [Fact]
    public void Classify_IsDeterministic_ForSameInputs()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY02", 1m),
            Balance("LY01", 2m)
        };
        var layers = new List<StockReconciliationClassifier.LedgerLayerSnapshot>
        {
            Layer("LY02", 1m, isDepleted: false),
            Layer("LY01", 2m, isDepleted: false),
            Layer("LY03", 0m, isDepleted: true)
        };

        var a = StockReconciliationClassifier.Classify(Scope, balances, layers);
        var b = StockReconciliationClassifier.Classify(Scope, balances, layers);

        a.Should().BeEquivalentTo(b);
        a.Outcome.Should().Be(StockReconciliationOutcomeEnum.IntentionalDifference);
        a.Differences.Select(d => d.LayananId).Should().Equal("LY03");
    }

    private static LegacyStockBalanceType Balance(string layananId, decimal qty)
        => new(
            BrgId: Scope.BrgId,
            ReceiptSourceId: Scope.ReceiptSourceId,
            LayananId: layananId,
            Quantity: qty,
            UnitCost: 1000m,
            ExpirationDate: new DateOnly(2025, 12, 31),
            Batch: "B1",
            PurchaseOrderId: null,
            LegacyRowId: "STK-" + layananId,
            ReceiptTime: T1,
            LastMutationTime: T1);

    private static StockReconciliationClassifier.LedgerLayerSnapshot Layer(
        string layananId,
        decimal remaining,
        bool isDepleted)
        => new(layananId, remaining, isDepleted);
}

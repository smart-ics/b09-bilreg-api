using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S4 — Baseline calculation + ambiguity classification (calculation only).
/// </summary>
public class LegacyReconstructionBaselineCalculatorTest
{
    private static readonly StockLedgerScopeKeyType Scope =
        StockLedgerScopeKeyType.Create("BRG01", "DO001");

    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T3 = new(2024, 2, 1, 9, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateOnly ExpA = new(2025, 12, 31);
    private static readonly DateOnly ExpB = new(2025, 6, 1);

    [Fact]
    public void UT01_MultiLocation_ItemReceiptSource_ProducesBalancedPositions()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 10m, 1000m, ExpA, "STK001", T1, "B1"),
            Balance("LY02", 7m, 1500m, ExpB, "STK002", T2, "B2")
        };
        var journals = new List<LegacyStockJournalEntryType>
        {
            Journal("TRS001", "LY01", qtyIn: 10m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
            Journal("TRS002", "LY02", qtyIn: 10m, qtyOut: 3m, 1500m, ExpB, T2, "B2")
        };

        var result = LegacyReconstructionBaselineCalculator.Calculate(Scope, balances, journals);

        result.IsBalanced.Should().BeTrue();
        result.InconsistencyReason.Should().BeNull();
        result.ProposedRemainingQuantity.Should().Be(17m);
        result.ProposedPositions.Should().HaveCount(2);
        result.EstablishingMovement.Should().NotBeNull();
        result.EstablishingMovement!.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        result.EstablishingMovement.MovementKind.Should().Be(StockMovementKindEnum.Receipt);
        result.EstablishingMovement.Lines.Should().HaveCount(2);
        result.EstablishingMovement.Lines.Should().OnlyContain(l => l.Origin == StockFactOriginEnum.Reconstructed);

        var ly01 = result.ProposedPositions.Single(p => p.LayananId == "LY01");
        ly01.TotalRemainingQuantity.Should().Be(10m);
        ly01.Layers.Should().ContainSingle();
        ly01.Layers[0].RemainingQuantity.Should().Be(10m);
        ly01.Layers[0].InitialQuantity.Should().Be(10m);
        ly01.Layers[0].Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        ly01.Layers[0].IsDepleted.Should().BeFalse();

        var ly02 = result.ProposedPositions.Single(p => p.LayananId == "LY02");
        ly02.TotalRemainingQuantity.Should().Be(7m);
        ly02.Layers.Should().ContainSingle();
        ly02.Layers[0].RemainingQuantity.Should().Be(7m);
        ly02.Layers[0].InitialQuantity.Should().Be(10m);
        ly02.Layers[0].IsDepleted.Should().BeFalse();

        ly01.LedgerScope.Should().Be(ly02.LedgerScope);
        ly01.WriteScope.Should().NotBe(ly02.WriteScope);
    }

    [Fact]
    public void UT02_DepletedLegacyRowsAbsentFromTbStok_RetainedAsDepletedLayers_Balanced()
    {
        // Surviving balance only at LY01; LY02 fully consumed and deleted from tb_stok.
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 5m, 1000m, ExpA, "STK001", T1, "B1")
        };
        var journals = new List<LegacyStockJournalEntryType>
        {
            Journal("TRS001", "LY01", qtyIn: 5m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
            Journal("TRS002", "LY02", qtyIn: 8m, qtyOut: 0m, 1200m, ExpB, T2, "B2"),
            Journal("TRS003", "LY02", qtyIn: 0m, qtyOut: 8m, 1200m, ExpB, T3, "B2")
        };

        var result = LegacyReconstructionBaselineCalculator.Calculate(Scope, balances, journals);

        result.IsBalanced.Should().BeTrue();
        result.ProposedRemainingQuantity.Should().Be(5m);
        result.ProposedPositions.Should().HaveCount(2);

        var ly01 = result.ProposedPositions.Single(p => p.LayananId == "LY01");
        ly01.Layers.Should().ContainSingle();
        ly01.Layers[0].IsDepleted.Should().BeFalse();
        ly01.Layers[0].RemainingQuantity.Should().Be(5m);

        var ly02 = result.ProposedPositions.Single(p => p.LayananId == "LY02");
        ly02.Layers.Should().ContainSingle();
        ly02.Layers[0].IsDepleted.Should().BeTrue();
        ly02.Layers[0].RemainingQuantity.Should().Be(0m);
        ly02.Layers[0].InitialQuantity.Should().Be(8m);
        ly02.Layers[0].Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        ly02.TotalRemainingQuantity.Should().Be(0m);

        result.EstablishingMovement!.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void UT03_QuantityMismatch_JournalNetVsBalance_IsInconsistent()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 10m, 1000m, ExpA, "STK001", T1, "B1")
        };
        var journals = new List<LegacyStockJournalEntryType>
        {
            Journal("TRS001", "LY01", qtyIn: 10m, qtyOut: 0m, 1000m, ExpA, T1, "B1"),
            Journal("TRS002", "LY01", qtyIn: 0m, qtyOut: 3m, 1000m, ExpA, T2, "B1")
            // journal net = 7, balance = 10 → mismatch
        };

        var result = LegacyReconstructionBaselineCalculator.Calculate(Scope, balances, journals);

        result.IsInconsistent.Should().BeTrue();
        result.Outcome.Should().Be(ReconstructionBaselineOutcomeEnum.Inconsistent);
        result.InconsistencyReason.Should().Contain("Quantity mismatch");
        result.InconsistencyReason.Should().Contain("LY01");
        result.EstablishingMovement.Should().BeNull();
        result.ProposedPositions.Should().BeEmpty();
        result.ProposedRemainingQuantity.Should().Be(0m);
    }

    [Fact]
    public void UT04_MaterialAmbiguity_MultipleBalancesShareProvenanceWithJournalHistory_IsInconsistent()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 4m, 1000m, ExpA, "STK001", T1, "B1"),
            Balance("LY01", 6m, 1000m, ExpA, "STK002", T1, "B1")
        };
        var journals = new List<LegacyStockJournalEntryType>
        {
            Journal("TRS001", "LY01", qtyIn: 12m, qtyOut: 2m, 1000m, ExpA, T1, "B1")
        };

        var result = LegacyReconstructionBaselineCalculator.Calculate(Scope, balances, journals);

        result.IsInconsistent.Should().BeTrue();
        result.InconsistencyReason.Should().Contain("Material ambiguity");
        result.InconsistencyReason.Should().Contain("BR-STL-105");
        result.EstablishingMovement.Should().BeNull();
        result.ProposedPositions.Should().BeEmpty();
    }

    [Fact]
    public void UT05_SameInput_RepeatedCalculation_IsDeterministic()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY02", 7m, 1500m, ExpB, "STK002", T2, "B2"),
            Balance("LY01", 10m, 1000m, ExpA, "STK001", T1, "B1")
        };
        var journals = new List<LegacyStockJournalEntryType>
        {
            Journal("TRS002", "LY02", qtyIn: 10m, qtyOut: 3m, 1500m, ExpB, T2, "B2"),
            Journal("TRS001", "LY01", qtyIn: 10m, qtyOut: 0m, 1000m, ExpA, T1, "B1")
        };

        var first = LegacyReconstructionBaselineCalculator.Calculate(Scope, balances, journals);
        var second = LegacyReconstructionBaselineCalculator.Calculate(
            Scope,
            balances.AsEnumerable().Reverse().ToList(),
            journals.AsEnumerable().Reverse().ToList());

        first.IsBalanced.Should().BeTrue();
        second.IsBalanced.Should().BeTrue();

        first.EstablishingMovement!.StockMovementId.Should().Be(second.EstablishingMovement!.StockMovementId);
        first.EstablishingMovement.SourceTransactionId.Should().Be(second.EstablishingMovement.SourceTransactionId);
        first.EstablishingMovement.Lines.Select(l => (l.LineNo, l.LayananId, l.Quantity, l.StockLayerId))
            .Should().Equal(second.EstablishingMovement.Lines.Select(l => (l.LineNo, l.LayananId, l.Quantity, l.StockLayerId)));

        first.ProposedPositions.Select(p => p.LayananId).Should().Equal(second.ProposedPositions.Select(p => p.LayananId));
        for (var i = 0; i < first.ProposedPositions.Count; i++)
        {
            var a = first.ProposedPositions[i];
            var b = second.ProposedPositions[i];
            a.Layers.Select(l => (l.StockLayerId, l.InitialQuantity, l.RemainingQuantity, l.EffectiveReceiptTime))
                .Should().Equal(b.Layers.Select(l => (l.StockLayerId, l.InitialQuantity, l.RemainingQuantity, l.EffectiveReceiptTime)));
        }
    }

    [Fact]
    public void UT06_BalancesOnly_NoJournals_UsesRemainingAsInitial_Balanced()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 3m, 500m, ExpA, "STK001", T1, null)
        };

        var result = LegacyReconstructionBaselineCalculator.Calculate(
            Scope,
            balances,
            Array.Empty<LegacyStockJournalEntryType>());

        result.IsBalanced.Should().BeTrue();
        result.UsedDeterministicOrderingFallback.Should().BeTrue();
        result.ProposedPositions.Should().ContainSingle();
        var layer = result.ProposedPositions[0].Layers.Single();
        layer.InitialQuantity.Should().Be(3m);
        layer.RemainingQuantity.Should().Be(3m);
        layer.Origin.Should().Be(StockFactOriginEnum.Reconstructed);
        // New accountable identity — not a legacy row id (BR-STL-066/067).
        // Persistable VARCHAR(26) deterministic id (not the legacy row id).
        layer.StockLayerId.Should().NotBe("STK001");
        layer.StockLayerId.Should().HaveLength(26);
        layer.StockLayerId.Should().Be(
            LegacyReconstructionBaselineCalculator.BuildLayerId(Scope, "LY01", 1));
    }

    [Fact]
    public void UT07_EmptySnapshot_IsBalancedWithNoPositions()
    {
        var result = LegacyReconstructionBaselineCalculator.Calculate(
            Scope,
            Array.Empty<LegacyStockBalanceType>(),
            Array.Empty<LegacyStockJournalEntryType>());

        result.IsBalanced.Should().BeTrue();
        result.EstablishingMovement.Should().BeNull();
        result.ProposedPositions.Should().BeEmpty();
        result.ProposedRemainingQuantity.Should().Be(0m);
    }

    [Fact]
    public void UT08_MissingBalanceWithPositiveJournalNet_IsInconsistent()
    {
        var journals = new List<LegacyStockJournalEntryType>
        {
            Journal("TRS001", "LY01", qtyIn: 5m, qtyOut: 0m, 1000m, ExpA, T1, "B1")
        };

        var result = LegacyReconstructionBaselineCalculator.Calculate(
            Scope,
            Array.Empty<LegacyStockBalanceType>(),
            journals);

        result.IsInconsistent.Should().BeTrue();
        result.InconsistencyReason.Should().Contain("Quantity mismatch");
        result.InconsistencyReason.Should().Contain("journal net 5");
        result.EstablishingMovement.Should().BeNull();
        result.ProposedPositions.Should().BeEmpty();
    }

    [Fact]
    public void UT09_DoesNotInventLegacyLayerIdentity_UsesSyntheticAccountableIds()
    {
        var balances = new List<LegacyStockBalanceType>
        {
            Balance("LY01", 2m, 1000m, ExpA, "LEGACY-LAYER-99", T1, "B1")
        };
        var journals = new List<LegacyStockJournalEntryType>
        {
            Journal("TRS001", "LY01", qtyIn: 2m, qtyOut: 0m, 1000m, ExpA, T1, "B1")
        };

        var result = LegacyReconstructionBaselineCalculator.Calculate(Scope, balances, journals);

        result.IsBalanced.Should().BeTrue();
        var layer = result.ProposedPositions.Single().Layers.Single();
        layer.StockLayerId.Should().Be(
            LegacyReconstructionBaselineCalculator.BuildLayerId(Scope, "LY01", 1));
        layer.StockLayerId.Should().HaveLength(26);
        layer.StockLayerId.Should().NotBe("LEGACY-LAYER-99");
        result.EstablishingMovement!.StockMovementId.Should().Be(
            LegacyReconstructionBaselineCalculator.BuildMovementId(Scope));
        result.EstablishingMovement.StockMovementId.Should().HaveLength(26);
    }

    private static LegacyStockBalanceType Balance(
        string layananId,
        decimal qty,
        decimal unitCost,
        DateOnly? exp,
        string? legacyRowId,
        DateTime receiptTime,
        string? batch)
        => new(
            BrgId: Scope.BrgId,
            ReceiptSourceId: Scope.ReceiptSourceId,
            LayananId: layananId,
            Quantity: qty,
            UnitCost: unitCost,
            ExpirationDate: exp,
            Batch: batch,
            PurchaseOrderId: null,
            LegacyRowId: legacyRowId,
            ReceiptTime: receiptTime,
            LastMutationTime: receiptTime);

    private static LegacyStockJournalEntryType Journal(
        string journalId,
        string layananId,
        decimal qtyIn,
        decimal qtyOut,
        decimal unitCost,
        DateOnly? exp,
        DateTime mutationTime,
        string? batch)
        => new(
            LegacyJournalId: journalId,
            BrgId: Scope.BrgId,
            ReceiptSourceId: Scope.ReceiptSourceId,
            LayananId: layananId,
            QuantityIn: qtyIn,
            QuantityOut: qtyOut,
            UnitCost: unitCost,
            ExpirationDate: exp,
            Batch: batch,
            MutationKindId: qtyIn > 0 ? "1" : "2",
            MutationTransactionId: "MUT-" + journalId,
            MutationTime: mutationTime,
            PurchaseOrderId: null);
}

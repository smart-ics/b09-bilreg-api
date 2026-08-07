using System.Reflection;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class LegacyReconstructionBasisCalculatorTest
{
    private static readonly DateTime T1 = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2024, 1, 16, 11, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void UT01_SameFixture_Twice_ProducesIdenticalOpaqueAndAlgorithmVersion()
    {
        var balances = SampleBalances();
        var journals = SampleJournals();

        var first = LegacyReconstructionBasisCalculator.Compute(balances, journals);
        var second = LegacyReconstructionBasisCalculator.Compute(balances, journals);

        first.Should().Be(second);
        first.AlgorithmVersion.Should().Be(LegacyReconstructionBasisCalculator.AlgorithmVersion);
        first.AlgorithmVersion.Should().Be("fingerprint-v1");
        first.OpaqueValue.Should().HaveCount(32);
    }

    [Fact]
    public void UT02_MaterialJournalChange_ProducesDifferentOpaqueValue()
    {
        var balances = SampleBalances();
        var journals = SampleJournals();
        var changedJournals = SampleJournals().ToList();
        changedJournals[0] = changedJournals[0] with { QuantityOut = 5m };

        var baseline = LegacyReconstructionBasisCalculator.Compute(balances, journals);
        var changed = LegacyReconstructionBasisCalculator.Compute(balances, changedJournals);

        changed.OpaqueValue.Should().NotEqual(baseline.OpaqueValue);
        changed.AlgorithmVersion.Should().Be(baseline.AlgorithmVersion);
    }

    [Fact]
    public void UT03_MaterialBalanceChange_ProducesDifferentOpaqueValue()
    {
        var balances = SampleBalances();
        var journals = SampleJournals();
        var changedBalances = SampleBalances().ToList();
        changedBalances[0] = changedBalances[0] with { Quantity = 99m };

        var baseline = LegacyReconstructionBasisCalculator.Compute(balances, journals);
        var changed = LegacyReconstructionBasisCalculator.Compute(changedBalances, journals);

        changed.OpaqueValue.Should().NotEqual(baseline.OpaqueValue);
    }

    [Fact]
    public void UT04_SameContent_DifferentInputOrder_ProducesSameFingerprint()
    {
        var balancesA = SampleBalances();
        var balancesB = SampleBalances().AsEnumerable().Reverse().ToList();
        var journalsA = SampleJournals();
        var journalsB = SampleJournals().AsEnumerable().Reverse().ToList();

        var a = LegacyReconstructionBasisCalculator.Compute(balancesA, journalsA);
        var b = LegacyReconstructionBasisCalculator.Compute(balancesB, journalsB);

        a.Should().Be(b);
    }

    [Fact]
    public void UT05_EmptyLists_StillProduceNonEmptyOpaquePosition()
    {
        var position = LegacyReconstructionBasisCalculator.Compute(
            Array.Empty<LegacyStockBalanceType>(),
            Array.Empty<LegacyStockJournalEntryType>());

        position.OpaqueValue.Should().NotBeEmpty();
        position.OpaqueValue.Should().HaveCount(32);
        position.AlgorithmVersion.Should().Be("fingerprint-v1");
    }

    [Fact]
    public void UT06_Result_UsableWithCompleteReconstruction_NoWatermarkMembers()
    {
        var position = LegacyReconstructionBasisCalculator.Compute(SampleBalances(), SampleJournals());

        var state = StockLedgerScopeStateModel
            .CreateNotReconstructed(StockLedgerScopeKeyType.Create("BRG01", "DO001"))
            .RequireReconstruction()
            .BeginReconstruction()
            .CompleteReconstruction(position, reconstructionBasisVersion: "fingerprint-v1");

        state.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructed);
        state.SynchronizationPosition.Should().Be(position);
        state.HasSynchronizationPosition.Should().BeTrue();
        state.ReconstructionBasisVersion.Should().Be("fingerprint-v1");

        var names = typeof(SynchronizationPositionType)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        names.Should().Contain("OpaqueValue");
        names.Should().Contain("AlgorithmVersion");
        names.Should().NotContain("MutationTime");
        names.Should().NotContain("FdTglJamMutasi");
        names.Should().NotContain("FsKdTrs");
        names.Should().NotContain("Watermark");
    }

    [Fact]
    public void UT07_NonMaterialBalanceTimestamps_DoNotChangeFingerprint()
    {
        var balancesA = SampleBalances();
        var balancesB = SampleBalances().ToList();
        balancesB[0] = balancesB[0] with
        {
            ReceiptTime = T1.AddDays(10),
            LastMutationTime = T2.AddHours(5)
        };

        var a = LegacyReconstructionBasisCalculator.Compute(balancesA, SampleJournals());
        var b = LegacyReconstructionBasisCalculator.Compute(balancesB, SampleJournals());

        a.Should().Be(b);
    }

    private static List<LegacyStockBalanceType> SampleBalances()
        =>
        [
            new LegacyStockBalanceType(
                BrgId: "BRG01",
                ReceiptSourceId: "DO001",
                LayananId: "LY02",
                Quantity: 7m,
                UnitCost: 1500m,
                ExpirationDate: new DateOnly(2025, 6, 1),
                Batch: "B2",
                PurchaseOrderId: "PO2",
                LegacyRowId: "STK002",
                ReceiptTime: T2,
                LastMutationTime: T2),
            new LegacyStockBalanceType(
                BrgId: "BRG01",
                ReceiptSourceId: "DO001",
                LayananId: "LY01",
                Quantity: 10m,
                UnitCost: 1000m,
                ExpirationDate: new DateOnly(2025, 12, 31),
                Batch: "B1",
                PurchaseOrderId: "PO1",
                LegacyRowId: "STK001",
                ReceiptTime: T1,
                LastMutationTime: T1)
        ];

    private static List<LegacyStockJournalEntryType> SampleJournals()
        =>
        [
            new LegacyStockJournalEntryType(
                LegacyJournalId: "TRS002",
                BrgId: "BRG01",
                ReceiptSourceId: "DO001",
                LayananId: "LY02",
                QuantityIn: 0m,
                QuantityOut: 3m,
                UnitCost: 1500m,
                ExpirationDate: new DateOnly(2025, 6, 1),
                Batch: "B2",
                MutationKindId: "2",
                MutationTransactionId: "MUT002",
                MutationTime: T2,
                PurchaseOrderId: "PO2"),
            new LegacyStockJournalEntryType(
                LegacyJournalId: "TRS001",
                BrgId: "BRG01",
                ReceiptSourceId: "DO001",
                LayananId: "LY01",
                QuantityIn: 10m,
                QuantityOut: 0m,
                UnitCost: 1000m,
                ExpirationDate: new DateOnly(2025, 12, 31),
                Batch: "B1",
                MutationKindId: "1",
                MutationTransactionId: "MUT001",
                MutationTime: T1,
                PurchaseOrderId: "PO1")
        ];
}

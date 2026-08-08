using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S1 / P4-S4 / P5-S2 — Live DM receipt/void and MT transfer post compatibility writer
/// against disposable <c>devTest</c>. Never writes <c>HOSPITAL_HPL</c>.
/// </summary>
public class LegacyCompatibilityWriterPortTest
{
    private static readonly DateTime MutationTime = new(2026, 8, 8, 10, 30, 0);
    private static readonly DateTime TransferTime = new(2026, 8, 8, 14, 0, 0);
    private static readonly DateOnly Expiration = new(2027, 6, 30);
    private const string Batch = "P4S1-BATCH";
    private const decimal UnitCost = 1500.50m;

    private readonly LegacyCompatibilityWriterPort _sut = new(ConnStringHelper.GetTestEnv());
    private readonly LegacyStockReadPort _readPort = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void Apply_ReceiptPost_ReadBack_MatchesQtyHppDoLocationEd()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("RB");
        Cleanup(ids);

        try
        {
            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 1));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            var journals = _readPort.ListJournalEntries(ids.Scope);

            balances.Should().ContainSingle();
            balances[0].BrgId.Should().Be(ids.BrgId);
            balances[0].ReceiptSourceId.Should().Be(ids.DoId);
            balances[0].LayananId.Should().Be(ids.LocationId);
            balances[0].Quantity.Should().Be(10m);
            balances[0].UnitCost.Should().Be(1500.50m);
            balances[0].ExpirationDate.Should().Be(new DateOnly(2027, 6, 30));
            balances[0].Batch.Should().Be("P4S1-BATCH");
            balances[0].PurchaseOrderId.Should().Be(ids.PoId);
            balances[0].LegacyRowId.Should().NotBeNullOrWhiteSpace();
            balances[0].LegacyRowId!.Should().StartWith("ST").And.HaveLength(10);

            journals.Should().ContainSingle();
            journals[0].QuantityIn.Should().Be(10m);
            journals[0].QuantityOut.Should().Be(0m);
            journals[0].UnitCost.Should().Be(1500.50m);
            journals[0].MutationKindId.Should().Be("DO");
            journals[0].MutationTransactionId.Should().Be(ids.DoId);
            journals[0].MutationTime.Should().Be(MutationTime);
            journals[0].LegacyJournalId.Should().StartWith("BK").And.HaveLength(10);
            journals[0].ExpirationDate.Should().Be(new DateOnly(2027, 6, 30));
            journals[0].Batch.Should().Be("P4S1-BATCH");
            journals[0].PurchaseOrderId.Should().Be(ids.PoId);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_ReceiptPost_ReceiptTimeNull_LastMutationTimeFromMutasi()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("RT");
        Cleanup(ids);

        try
        {
            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 1));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            balances.Should().ContainSingle();
            // VB6 parity: fd_tgl_do / fs_jam_do left at schema defaults → ReceiptTime null.
            balances[0].ReceiptTime.Should().BeNull();
            balances[0].LastMutationTime.Should().Be(MutationTime);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_ReceiptPost_FingerprintV1_ComputesStableHash()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("FP");
        Cleanup(ids);

        try
        {
            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 1));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            var journals = _readPort.ListJournalEntries(ids.Scope);
            var first = LegacyReconstructionBasisCalculator.Compute(balances, journals);
            var second = LegacyReconstructionBasisCalculator.Compute(balances, journals);

            first.AlgorithmVersion.Should().Be(LegacyReconstructionBasisCalculator.AlgorithmVersion);
            first.OpaqueValue.Should().HaveCount(32);
            first.Should().Be(second);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_WithoutComplete_RollsBackLegacyRows()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("RL");
        Cleanup(ids);

        try
        {
            using (TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 1));
                // dispose without Complete → rollback
            }

            _readPort.ListCurrentBalances(ids.Scope).Should().BeEmpty();
            _readPort.ListJournalEntries(ids.Scope).Should().BeEmpty();
            CountLegacyRows(ids).Should().Be((0, 0));
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_UnsupportedMovementKind_Throws()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("UK");
        var request = BuildReceiptRequest(ids, lineCount: 1) with
        {
            MovementKind = StockMovementKindEnum.Outbound
        };

        var act = () => _sut.Apply(request);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Receipt post, Reversal void, and Transfer post only*");
    }

    [Fact]
    public void Apply_TransferPost_ReadBack_ConservesQtyDoHppEdAtBothLocations()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("TC");
        Cleanup(ids);

        try
        {
            SeedSourceStock(ids, qty: 10m);
            var sourceStokId = _readPort.ListCurrentBalances(ids.Scope)
                .Single(b => b.LayananId == ids.LocationId).LegacyRowId!;
            var mtId = $"MT{ids.DoId}"[..10];

            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildTransferPostRequest(
                    ids, sourceStokId, ids.LocationId, ids.LocationId2, mtId, qty: 10m));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            balances.Should().ContainSingle(b => b.LayananId == ids.LocationId2);
            balances.Should().NotContain(b => b.LayananId == ids.LocationId);

            var dest = balances.Single(b => b.LayananId == ids.LocationId2);
            dest.Quantity.Should().Be(10m);
            dest.ReceiptSourceId.Should().Be(ids.DoId);
            dest.UnitCost.Should().Be(UnitCost);
            dest.ExpirationDate.Should().Be(Expiration);
            dest.Batch.Should().Be(Batch);
            dest.PurchaseOrderId.Should().Be(ids.PoId);

            var journals = _readPort.ListJournalEntries(ids.Scope);
            journals.Should().Contain(j =>
                j.MutationKindId == "MT_OUT"
                && j.LayananId == ids.LocationId
                && j.QuantityOut == 10m
                && j.QuantityIn == 0m
                && j.MutationTransactionId == mtId
                && j.UnitCost == UnitCost
                && j.ExpirationDate == Expiration
                && j.Batch == Batch
                && j.PurchaseOrderId == ids.PoId);
            journals.Should().Contain(j =>
                j.MutationKindId == "MT_IN"
                && j.LayananId == ids.LocationId2
                && j.QuantityIn == 10m
                && j.QuantityOut == 0m
                && j.MutationTransactionId == mtId
                && j.UnitCost == UnitCost
                && j.ExpirationDate == Expiration
                && j.Batch == Batch
                && j.PurchaseOrderId == ids.PoId);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_TransferPost_FullDepletion_DeletesSourceStokRow()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("TF");
        Cleanup(ids);

        try
        {
            SeedSourceStock(ids, qty: 10m);
            var sourceStokId = _readPort.ListCurrentBalances(ids.Scope)
                .Single(b => b.LayananId == ids.LocationId).LegacyRowId!;
            var mtId = $"MT{ids.DoId}"[..10];

            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildTransferPostRequest(
                    ids, sourceStokId, ids.LocationId, ids.LocationId2, mtId, qty: 10m));
                trans.Complete();
            }

            _readPort.ListCurrentBalances(ids.Scope)
                .Should().NotContain(b => b.LayananId == ids.LocationId);
            _readPort.ListCurrentBalances(ids.Scope)
                .Should().ContainSingle(b => b.LayananId == ids.LocationId2 && b.Quantity == 10m);
            CountLegacyRows(ids).Should().Be((1, 3)); // DO + MT_OUT + MT_IN journals; 1 dest stok
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_TransferPost_PartialDepletion_ReducesSourceQty()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("TP");
        Cleanup(ids);

        try
        {
            SeedSourceStock(ids, qty: 10m);
            var sourceStokId = _readPort.ListCurrentBalances(ids.Scope)
                .Single(b => b.LayananId == ids.LocationId).LegacyRowId!;
            var mtId = $"MT{ids.DoId}"[..10];

            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildTransferPostRequest(
                    ids, sourceStokId, ids.LocationId, ids.LocationId2, mtId, qty: 4m,
                    action: LegacyBalanceMutationActionEnum.Upsert));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            balances.Should().ContainSingle(b => b.LayananId == ids.LocationId && b.Quantity == 6m)
                .Which.LegacyRowId.Should().Be(sourceStokId);
            balances.Should().ContainSingle(b => b.LayananId == ids.LocationId2 && b.Quantity == 4m);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_TransferPost_WithoutComplete_RollsBackBothLegs()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("TR");
        Cleanup(ids);

        try
        {
            SeedSourceStock(ids, qty: 10m);
            var sourceStokId = _readPort.ListCurrentBalances(ids.Scope)
                .Single(b => b.LayananId == ids.LocationId).LegacyRowId!;
            var mtId = $"MT{ids.DoId}"[..10];

            using (TransHelper.NewScope())
            {
                _sut.Apply(BuildTransferPostRequest(
                    ids, sourceStokId, ids.LocationId, ids.LocationId2, mtId, qty: 10m));
                // dispose without Complete → rollback both OUT and IN
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            balances.Should().ContainSingle(b => b.LayananId == ids.LocationId && b.Quantity == 10m);
            balances.Should().NotContain(b => b.LayananId == ids.LocationId2);
            _readPort.ListJournalEntries(ids.Scope)
                .Should().ContainSingle(j => j.MutationKindId == "DO");
            _readPort.ListJournalEntries(ids.Scope)
                .Should().NotContain(j => j.MutationKindId == "MT_OUT" || j.MutationKindId == "MT_IN");
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_TransferPost_MultiSlice_TwoOutInPairs()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("TM");
        Cleanup(ids);

        try
        {
            // Two source rows at same location (two DO receipts → two ST rows for same DO via two posts).
            // Use one DO with two distinct ST rows by seeding twice is not possible for same DO+location
            // without merge; instead seed full qty then transfer two partial slices from same ST.
            SeedSourceStock(ids, qty: 10m);
            var sourceStokId = _readPort.ListCurrentBalances(ids.Scope)
                .Single(b => b.LayananId == ids.LocationId).LegacyRowId!;
            var mtId = $"MT{ids.DoId}"[..10];

            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildMultiSliceTransferRequest(
                    ids,
                    sourceStokId,
                    ids.LocationId,
                    ids.LocationId2,
                    mtId,
                    sliceQtys: [3m, 7m]));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            balances.Should().NotContain(b => b.LayananId == ids.LocationId);
            balances.Where(b => b.LayananId == ids.LocationId2).Sum(b => b.Quantity).Should().Be(10m);
            balances.Where(b => b.LayananId == ids.LocationId2).Should().HaveCount(2);

            var journals = _readPort.ListJournalEntries(ids.Scope);
            journals.Count(j => j.MutationKindId == "MT_OUT").Should().Be(2);
            journals.Count(j => j.MutationKindId == "MT_IN").Should().Be(2);
            journals.Where(j => j.MutationKindId == "MT_OUT").Sum(j => j.QuantityOut).Should().Be(10m);
            journals.Where(j => j.MutationKindId == "MT_IN").Sum(j => j.QuantityIn).Should().Be(10m);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_ReceiptVoid_ProjectedFingerprint_MatchesReadBack()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("PF");
        Cleanup(ids);

        try
        {
            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 1));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            var journals = _readPort.ListJournalEntries(ids.Scope);
            var stokId = balances.Single().LegacyRowId!;
            var voidTime = MutationTime.AddHours(1);
            var voidWrite = BuildVoidRequest(ids, stokId, quantity: 10m, voidTime);

            var (projectedBalances, projectedJournals) =
                DoReceiptVoidLegacyCompatibilityMapper.ProjectPostVoidFingerprintSnapshot(
                    balances, journals, voidWrite);
            var projected = LegacyReconstructionBasisCalculator.Compute(
                projectedBalances, projectedJournals);

            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(voidWrite);
                trans.Complete();
            }

            var actualBalances = _readPort.ListCurrentBalances(ids.Scope);
            var actualJournals = _readPort.ListJournalEntries(ids.Scope);
            var actual = LegacyReconstructionBasisCalculator.Compute(actualBalances, actualJournals);

            actualBalances.Should().BeEmpty();
            actualJournals.Should().HaveCount(2);
            projected.Should().Be(actual);
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_ReceiptVoid_DeletesBalance_AndInsertsDoVJournal()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("VV");
        Cleanup(ids);

        try
        {
            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 1));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            balances.Should().ContainSingle();
            var stokId = balances[0].LegacyRowId!;
            var voidTime = MutationTime.AddHours(1);

            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildVoidRequest(ids, stokId, quantity: 10m, voidTime));
                trans.Complete();
            }

            _readPort.ListCurrentBalances(ids.Scope).Should().BeEmpty();
            var journals = _readPort.ListJournalEntries(ids.Scope);
            journals.Should().HaveCount(2);
            journals.Should().Contain(j => j.MutationKindId == "DO" && j.QuantityIn == 10m);
            journals.Should().Contain(j =>
                j.MutationKindId == "DO_V"
                && j.QuantityOut == 10m
                && j.QuantityIn == 0m
                && j.MutationTime == voidTime);
            CountLegacyRows(ids).Should().Be((0, 2));
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_ReceiptVoid_WithoutComplete_RollsBackVoid()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("VR");
        Cleanup(ids);

        try
        {
            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 1));
                trans.Complete();
            }

            var stokId = _readPort.ListCurrentBalances(ids.Scope).Single().LegacyRowId!;

            using (TransHelper.NewScope())
            {
                _sut.Apply(BuildVoidRequest(ids, stokId, quantity: 10m, MutationTime.AddHours(1)));
                // dispose without Complete → rollback void
            }

            _readPort.ListCurrentBalances(ids.Scope).Should().ContainSingle()
                .Which.Quantity.Should().Be(10m);
            _readPort.ListJournalEntries(ids.Scope).Should().ContainSingle()
                .Which.MutationKindId.Should().Be("DO");
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_ReceiptPost_GeneratesUniqueBkStIds()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("ID");
        Cleanup(ids);

        try
        {
            using (var trans = TransHelper.NewScope())
            {
                _sut.Apply(BuildReceiptRequest(ids, lineCount: 2));
                trans.Complete();
            }

            var balances = _readPort.ListCurrentBalances(ids.Scope);
            var journals = _readPort.ListJournalEntries(ids.Scope);

            balances.Should().HaveCount(2);
            journals.Should().HaveCount(2);

            var stokIds = balances.Select(x => x.LegacyRowId!).ToList();
            var bukuIds = journals.Select(x => x.LegacyJournalId).ToList();

            stokIds.Should().OnlyContain(x => x.StartsWith("ST") && x.Length == 10);
            bukuIds.Should().OnlyContain(x => x.StartsWith("BK") && x.Length == 10);
            stokIds.Concat(bukuIds).Should().OnlyHaveUniqueItems();
        }
        finally
        {
            Cleanup(ids);
        }
    }

    [Fact]
    public void Apply_MismatchedJournalBalanceCounts_Throws()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        var ids = CreateCaseIds("MM");
        var balanced = BuildReceiptRequest(ids, lineCount: 1);
        var request = balanced with
        {
            JournalEntries = Array.Empty<LegacyCompatibilityJournalEntryType>()
        };

        var act = () => _sut.Apply(request);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*paired 1:1*");
    }

    private void SeedSourceStock(CaseIds ids, decimal qty)
    {
        using var trans = TransHelper.NewScope();
        _sut.Apply(BuildReceiptRequest(ids, lineCount: 1, quantity: qty));
        trans.Complete();
    }

    private static LegacyCompatibilityWriteRequest BuildTransferPostRequest(
        CaseIds ids,
        string sourceStokId,
        string sourceLocationId,
        string destLocationId,
        string mtId,
        decimal qty,
        LegacyBalanceMutationActionEnum action = LegacyBalanceMutationActionEnum.Delete)
    {
        return BuildMultiSliceTransferRequest(
            ids,
            sourceStokId,
            sourceLocationId,
            destLocationId,
            mtId,
            [qty],
            singleSliceOutAction: action);
    }

    private static LegacyCompatibilityWriteRequest BuildMultiSliceTransferRequest(
        CaseIds ids,
        string sourceStokId,
        string sourceLocationId,
        string destLocationId,
        string mtId,
        IReadOnlyList<decimal> sliceQtys,
        LegacyBalanceMutationActionEnum? singleSliceOutAction = null)
    {
        var balances = new List<LegacyCompatibilityBalanceMutationType>();
        var journals = new List<LegacyCompatibilityJournalEntryType>();

        // Interleaved OUT/IN pairs; writer processes all OUT first, then all IN.
        for (var i = 0; i < sliceQtys.Count; i++)
        {
            var qty = sliceQtys[i];
            LegacyBalanceMutationActionEnum outAction;
            if (sliceQtys.Count == 1)
            {
                outAction = singleSliceOutAction ?? LegacyBalanceMutationActionEnum.Delete;
            }
            else
            {
                // Multi-slice from one ST row: Upsert until last slice, then Delete.
                outAction = i == sliceQtys.Count - 1
                    ? LegacyBalanceMutationActionEnum.Delete
                    : LegacyBalanceMutationActionEnum.Upsert;
            }

            var outBukuId = Nuna.Lib.AutoNumberHelper.NunaId.NewLegacyCompact("BK");
            var inBukuId = Nuna.Lib.AutoNumberHelper.NunaId.NewLegacyCompact("BK");
            var inStokId = Nuna.Lib.AutoNumberHelper.NunaId.NewLegacyCompact("ST");

            balances.Add(new LegacyCompatibilityBalanceMutationType(
                outAction,
                ids.BrgId,
                ids.DoId,
                sourceLocationId,
                Quantity: qty,
                UnitCost: UnitCost,
                ExpirationDate: Expiration,
                Batch: Batch,
                PurchaseOrderId: ids.PoId,
                LegacyRowId: sourceStokId,
                SmallestUnitId: "TAB"));
            journals.Add(new LegacyCompatibilityJournalEntryType(
                LegacyJournalId: outBukuId,
                BrgId: ids.BrgId,
                ReceiptSourceId: ids.DoId,
                LayananId: sourceLocationId,
                QuantityIn: 0m,
                QuantityOut: qty,
                UnitCost: UnitCost,
                ExpirationDate: Expiration,
                Batch: Batch,
                MutationKindId: "MT_OUT",
                MutationTransactionId: mtId,
                MutationTime: TransferTime,
                IsVoid: false,
                SmallestUnitId: "TAB"));

            balances.Add(new LegacyCompatibilityBalanceMutationType(
                LegacyBalanceMutationActionEnum.Upsert,
                ids.BrgId,
                ids.DoId,
                destLocationId,
                Quantity: qty,
                UnitCost: UnitCost,
                ExpirationDate: Expiration,
                Batch: Batch,
                PurchaseOrderId: ids.PoId,
                LegacyRowId: inStokId,
                SmallestUnitId: "TAB"));
            journals.Add(new LegacyCompatibilityJournalEntryType(
                LegacyJournalId: inBukuId,
                BrgId: ids.BrgId,
                ReceiptSourceId: ids.DoId,
                LayananId: destLocationId,
                QuantityIn: qty,
                QuantityOut: 0m,
                UnitCost: UnitCost,
                ExpirationDate: Expiration,
                Batch: Batch,
                MutationKindId: "MT_IN",
                MutationTransactionId: mtId,
                MutationTime: TransferTime,
                IsVoid: false,
                SmallestUnitId: "TAB"));
        }

        return new LegacyCompatibilityWriteRequest(
            SourceTransactionReferenceType.Key($"MT-{ids.SourceTxId}"),
            StockMovementKindEnum.Transfer,
            ids.Scope,
            balances,
            journals);
    }

    private static LegacyCompatibilityWriteRequest BuildVoidRequest(
        CaseIds ids,
        string stokId,
        decimal quantity,
        DateTime voidTime)
    {
        var bukuId = Nuna.Lib.AutoNumberHelper.NunaId.NewLegacyCompact("BK");
        return new LegacyCompatibilityWriteRequest(
            SourceTransactionReferenceType.Key($"VOID-{ids.SourceTxId}"),
            StockMovementKindEnum.Reversal,
            ids.Scope,
            [
                new LegacyCompatibilityBalanceMutationType(
                    LegacyBalanceMutationActionEnum.Delete,
                    ids.BrgId,
                    ids.DoId,
                    ids.LocationId,
                    Quantity: quantity,
                    UnitCost: UnitCost,
                    ExpirationDate: Expiration,
                    Batch: Batch,
                    PurchaseOrderId: ids.PoId,
                    LegacyRowId: stokId,
                    SmallestUnitId: "TAB")
            ],
            [
                new LegacyCompatibilityJournalEntryType(
                    LegacyJournalId: bukuId,
                    BrgId: ids.BrgId,
                    ReceiptSourceId: ids.DoId,
                    LayananId: ids.LocationId,
                    QuantityIn: 0m,
                    QuantityOut: quantity,
                    UnitCost: UnitCost,
                    ExpirationDate: Expiration,
                    Batch: Batch,
                    MutationKindId: "DO_V",
                    MutationTransactionId: ids.DoId,
                    MutationTime: voidTime,
                    IsVoid: true,
                    SmallestUnitId: "TAB")
            ]);
    }

    private static LegacyCompatibilityWriteRequest BuildReceiptRequest(
        CaseIds ids,
        int lineCount,
        decimal? quantity = null)
    {
        var balances = new List<LegacyCompatibilityBalanceMutationType>();
        var journals = new List<LegacyCompatibilityJournalEntryType>();

        for (var i = 0; i < lineCount; i++)
        {
            var location = i == 0 ? ids.LocationId : ids.LocationId2;
            var qty = quantity ?? (10m + i);
            balances.Add(new LegacyCompatibilityBalanceMutationType(
                LegacyBalanceMutationActionEnum.Upsert,
                ids.BrgId,
                ids.DoId,
                location,
                Quantity: qty,
                UnitCost: UnitCost,
                ExpirationDate: Expiration,
                Batch: Batch,
                PurchaseOrderId: ids.PoId,
                LegacyRowId: null,
                SmallestUnitId: "TAB"));

            journals.Add(new LegacyCompatibilityJournalEntryType(
                LegacyJournalId: string.Empty,
                BrgId: ids.BrgId,
                ReceiptSourceId: ids.DoId,
                LayananId: location,
                QuantityIn: qty,
                QuantityOut: 0m,
                UnitCost: UnitCost,
                ExpirationDate: Expiration,
                Batch: Batch,
                MutationKindId: "DO",
                MutationTransactionId: ids.DoId,
                MutationTime: MutationTime,
                IsVoid: false,
                SmallestUnitId: "TAB"));
        }

        return new LegacyCompatibilityWriteRequest(
            SourceTransactionReferenceType.Key(ids.SourceTxId),
            StockMovementKindEnum.Receipt,
            ids.Scope,
            balances,
            journals);
    }

    private static CaseIds CreateCaseIds(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 2 ? tag : tag[..2];
        // Column widths: BrgId VARCHAR(13), ReceiptSourceId VARCHAR(10), LayananId VARCHAR(5), PO VARCHAR(10).
        var brgId = $"BRG{shortTag}{ulid}"[..13];
        var doId = $"DO{shortTag}{ulid}"[..10];
        return new CaseIds(
            SourceTxId: $"P5S2-{tag}-{ulid}",
            BrgId: brgId,
            DoId: doId,
            PoId: $"PO{shortTag}{ulid}"[..10],
            LocationId: $"G{shortTag}{ulid}"[..5],
            LocationId2: $"H{shortTag}{ulid}"[..5],
            Scope: StockLedgerScopeKeyType.Create(brgId, doId));
    }

    private static (int Stok, int Buku) CountLegacyRows(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        var stok = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM tb_stok WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId",
            new { ids.BrgId, ids.DoId });
        var buku = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM tb_buku WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId",
            new { ids.BrgId, ids.DoId });
        return (stok, buku);
    }

    private static void Cleanup(CaseIds ids)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        conn.Execute(
            """
            DELETE FROM tb_stok WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            DELETE FROM tb_buku WHERE fs_kd_barang = @BrgId AND fs_kd_do = @DoId;
            """,
            new { ids.BrgId, ids.DoId });
    }

    private sealed record CaseIds(
        string SourceTxId,
        string BrgId,
        string DoId,
        string PoId,
        string LocationId,
        string LocationId2,
        IStockLedgerScopeKey Scope);
}

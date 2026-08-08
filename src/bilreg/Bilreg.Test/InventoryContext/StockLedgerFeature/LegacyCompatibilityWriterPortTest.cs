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
/// P4-S1 — Live DM receipt post compatibility writer against disposable <c>devTest</c>.
/// Never writes <c>HOSPITAL_HPL</c>.
/// </summary>
public class LegacyCompatibilityWriterPortTest
{
    private static readonly DateTime MutationTime = new(2026, 8, 8, 10, 30, 0);

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
            .WithMessage("*Receipt post only*");
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

    private static LegacyCompatibilityWriteRequest BuildReceiptRequest(CaseIds ids, int lineCount)
    {
        var balances = new List<LegacyCompatibilityBalanceMutationType>();
        var journals = new List<LegacyCompatibilityJournalEntryType>();

        for (var i = 0; i < lineCount; i++)
        {
            var location = i == 0 ? ids.LocationId : ids.LocationId2;
            var qty = 10m + i;
            balances.Add(new LegacyCompatibilityBalanceMutationType(
                LegacyBalanceMutationActionEnum.Upsert,
                ids.BrgId,
                ids.DoId,
                location,
                Quantity: qty,
                UnitCost: 1500.50m,
                ExpirationDate: new DateOnly(2027, 6, 30),
                Batch: "P4S1-BATCH",
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
                UnitCost: 1500.50m,
                ExpirationDate: new DateOnly(2027, 6, 30),
                Batch: "P4S1-BATCH",
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
            SourceTxId: $"P4S1-{tag}-{ulid}",
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

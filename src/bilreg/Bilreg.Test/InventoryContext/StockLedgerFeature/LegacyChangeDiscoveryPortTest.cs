using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StokFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S1 / G-13 — Live Legacy Change Discovery detection experiments on disposable fixtures.
/// </summary>
public class LegacyChangeDiscoveryPortTest
{
    private readonly LegacyChangeDiscoveryPort _sut;
    private readonly LegacyStockReadPort _legacyRead;
    private readonly StockSourceIdempotencyRepo _idempotencyRepo;
    private readonly tb_stok_dal _stokDal;
    private readonly tb_buku_dal _bukuDal;

    private static readonly DateTime T1 = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime T2 = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Unspecified);

    public LegacyChangeDiscoveryPortTest()
    {
        var options = ConnStringHelper.GetTestEnv();
        _legacyRead = new LegacyStockReadPort(options);
        var idempotencyDal = new StockSourceIdempotencyDal(options);
        _idempotencyRepo = new StockSourceIdempotencyRepo(idempotencyDal);
        _sut = new LegacyChangeDiscoveryPort(_legacyRead, idempotencyDal);
        _stokDal = new tb_stok_dal(options);
        _bukuDal = new tb_buku_dal(options);
    }

    [Fact]
    public void ComputeCurrentFingerprint_UsesFingerprintV1Calculator()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("FP1");
        SeedBaseline(scope);

        var fingerprint = _sut.ComputeCurrentFingerprint(scope);

        fingerprint.AlgorithmVersion.Should().Be(LegacyReconstructionBasisCalculator.AlgorithmVersion);
        var balances = _legacyRead.ListCurrentBalances(scope);
        var journals = _legacyRead.ListJournalEntries(scope);
        fingerprint.Should().Be(LegacyReconstructionBasisCalculator.Compute(balances, journals));
    }

    [Fact]
    public void FingerprintContinuity_MatchesReconstructionInitializedPosition()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("CNT");
        SeedBaseline(scope);
        SeedKnownIdentities(scope);

        var stored = _sut.ComputeCurrentFingerprint(scope);
        var result = _sut.DiscoverChanges(scope, stored);

        result.Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.Unchanged);
        result.CurrentFingerprint.Should().Be(stored);
    }

    [Fact]
    public void Unchanged_WhenLegacyAuthorityMatchesStoredPosition()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("UNC");
        SeedBaseline(scope);
        SeedKnownIdentities(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);

        _sut.DiscoverChanges(scope, stored).Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.Unchanged);
    }

    [Fact]
    public void JournalInsert_IsDetected()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("INS");
        SeedBaseline(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);
        SeedKnownIdentities(scope);

        _bukuDal.Insert([
            BukuRow(scope, "BK731INS", "G731A", 1m, 0m, "DU", "FO731INS", "2026-03-03 10:00:00")
        ]);

        var result = _sut.DiscoverChanges(scope, stored);

        result.Outcome.Should().Be(LegacyChangeDiscoveryOutcomeEnum.ChangesDetected);
        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalInsert
                                            && x.LegacyJournalId == "BK731INS");
    }

    [Fact]
    public void JournalVoidDelete_IsDetected()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("VOD");
        SeedBaseline(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);
        SeedKnownIdentities(scope);

        DeleteBuku(scope, "BK731A2");

        var result = _sut.DiscoverChanges(scope, stored);

        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalVoidDelete
                                            && x.LegacyJournalId == "BK731A2");
    }

    [Fact]
    public void JournalUpdate_IsDetected_ForBackdatedMutationTime()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("JUP");
        SeedBaseline(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);
        SeedKnownIdentities(scope);

        DeleteBuku(scope, "BK731A1");
        _bukuDal.Insert([
            BukuRow(scope, "BK731A1", "G731A", 10m, 0m, "DO", scope.ReceiptSourceId, "2026-02-28 23:59:59")
        ]);

        var result = _sut.DiscoverChanges(scope, stored);

        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.JournalUpdate
                                            && x.LegacyJournalId == "BK731A1");
    }

    [Fact]
    public void BalanceDelete_IsDetected_WhenDepletedRowRemoved()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("BDE");
        SeedBaseline(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);
        SeedKnownIdentities(scope);

        DeleteStok(scope, "ST731B");

        var result = _sut.DiscoverChanges(scope, stored);

        result.Deltas.Should().Contain(x => x.Kind == LegacyDiscoveredDeltaKindEnum.BalanceDelete
                                            && x.LegacyJournalId == "ST731B");
    }

    [Fact]
    public void BalanceUpdate_IsDetected()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("BUP");
        SeedBaseline(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);
        SeedKnownIdentities(scope);

        DeleteStok(scope, "ST731A");
        _stokDal.Insert(StokRow(scope, "ST731A", "G731A", 9m));

        var result = _sut.DiscoverChanges(scope, stored);

        result.Deltas.Should().ContainSingle(x => x.Kind == LegacyDiscoveredDeltaKindEnum.BalanceUpdate);
    }

    [Fact]
    public void RequiresScopedReDerive_WhenFingerprintMismatchWithoutLedgerKnownIdentities()
    {
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("RSD");
        SeedBaseline(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);

        DeleteStok(scope, "ST731A");
        _stokDal.Insert(StokRow(scope, "ST731A", "G731A", 9m));

        var result = _sut.DiscoverChanges(scope, stored);

        result.Deltas.Should().ContainSingle(x => x.Kind == LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive);
    }

    [Fact]
    public void DiscoverChanges_IsReadOnly_DoesNotMutateLegacyOrLedgerTables()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();
        using var trans = TransHelper.NewScope();
        var scope = NewScope("RO");
        SeedBaseline(scope);
        SeedKnownIdentities(scope);
        var stored = _sut.ComputeCurrentFingerprint(scope);

        var balancesBefore = _legacyRead.ListCurrentBalances(scope).ToList();
        var journalsBefore = _legacyRead.ListJournalEntries(scope).ToList();
        var idempotencyCountBefore = CountSyncIdentityKeys(scope);

        _ = _sut.DiscoverChanges(scope, stored);
        _ = _sut.ComputeCurrentFingerprint(scope);

        _legacyRead.ListCurrentBalances(scope).Should().BeEquivalentTo(balancesBefore, opt => opt.WithStrictOrdering());
        _legacyRead.ListJournalEntries(scope).Should().BeEquivalentTo(journalsBefore, opt => opt.WithStrictOrdering());
        CountSyncIdentityKeys(scope).Should().Be(idempotencyCountBefore);
    }

    private void SeedBaseline(IStockLedgerScopeKey scope)
    {
        _stokDal.Insert(StokRow(scope, "ST731A", "G731A", 10m));
        _stokDal.Insert(StokRow(scope, "ST731B", "G731B", 5m));
        _bukuDal.Insert([
            BukuRow(scope, "BK731A1", "G731A", 10m, 0m, "DO", scope.ReceiptSourceId, "2026-03-01 08:00:00"),
            BukuRow(scope, "BK731A2", "G731A", 0m, 2m, "DU", "FO731A", "2026-03-01 10:00:00"),
            BukuRow(scope, "BK731B1", "G731B", 5m, 0m, "DO", scope.ReceiptSourceId, "2026-03-02 09:00:00")
        ]);
    }

    private void SeedKnownIdentities(IStockLedgerScopeKey scope)
    {
        var balances = _legacyRead.ListCurrentBalances(scope);
        var journals = _legacyRead.ListJournalEntries(scope);
        var processedAt = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Unspecified);

        foreach (var journal in journals)
        {
            _idempotencyRepo.InsertOrGetExisting(
                StockSourceIdempotencyModel.Create(
                    StockSourceIdempotencyKindEnum.SyncBatch,
                    LegacyChangeDiscoveryIdentityKeys.BuildJournalKey(scope, journal),
                    processedAt,
                    brgId: scope.BrgId,
                    receiptSourceId: scope.ReceiptSourceId));
        }

        foreach (var balance in balances.Where(x => !string.IsNullOrWhiteSpace(x.LegacyRowId)))
        {
            _idempotencyRepo.InsertOrGetExisting(
                StockSourceIdempotencyModel.Create(
                    StockSourceIdempotencyKindEnum.SyncBatch,
                    LegacyChangeDiscoveryIdentityKeys.BuildBalanceKey(scope, balance),
                    processedAt,
                    brgId: scope.BrgId,
                    receiptSourceId: scope.ReceiptSourceId));
        }
    }

    private static IStockLedgerScopeKey NewScope(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        return StockLedgerScopeKeyType.Create(
            $"BRG731{tag}{ulid}"[..13],
            $"DO731{tag}{ulid}"[..10]);
    }

    private static tb_stok_dto StokRow(
        IStockLedgerScopeKey scope,
        string fs_kd_trs,
        string fs_kd_layanan,
        decimal fn_qty)
        => new(
            fs_kd_trs: fs_kd_trs,
            fs_kd_barang: scope.BrgId,
            fs_kd_layanan: fs_kd_layanan,
            fs_kd_po: "PO731",
            fs_kd_do: scope.ReceiptSourceId,
            fd_tgl_ed: "2027-01-01",
            fs_no_batch: "B731",
            fn_qty_in: fn_qty,
            fn_qty: fn_qty,
            fn_hpp: 1000m,
            fd_tgl_do: "2026-03-01",
            fs_jam_do: "08:00:00",
            fs_kd_mutasi: scope.ReceiptSourceId,
            fd_tgl_mutasi: "2026-03-01",
            fs_jam_mutasi: "08:00:00",
            fs_kd_satuan: "TAB",
            fs_nm_barang: string.Empty,
            fs_nm_layanan: string.Empty);

    private static tb_buku_dto BukuRow(
        IStockLedgerScopeKey scope,
        string fs_kd_trs,
        string fs_kd_layanan,
        decimal fn_stok_in,
        decimal fn_stok_out,
        string fs_kd_jenis_mutasi,
        string fs_kd_mutasi,
        string fd_tgl_jam_mutasi)
        => new(
            fs_kd_trs: fs_kd_trs,
            fs_kd_barang: scope.BrgId,
            fs_kd_layanan: fs_kd_layanan,
            fs_kd_po: "PO731",
            fs_kd_do: scope.ReceiptSourceId,
            fd_tgl_ed: "2027-01-01",
            fs_no_batch: "B731",
            fn_stok_in: fn_stok_in,
            fn_stok_out: fn_stok_out,
            fn_hpp: 1000m,
            fs_kd_jenis_mutasi: fs_kd_jenis_mutasi,
            fs_kd_mutasi: fs_kd_mutasi,
            fd_tgl_jam_mutasi: fd_tgl_jam_mutasi,
            fd_tgl_mutasi: fd_tgl_jam_mutasi[..10],
            fs_jam_mutasi: fd_tgl_jam_mutasi[11..],
            fs_kd_satuan: "TAB",
            fs_nm_barang: string.Empty,
            fs_nm_layanan: string.Empty);

    private void DeleteBuku(IStockLedgerScopeKey scope, string fs_kd_trs)
        => _bukuDal.Delete(new BukuDeleteKey(fs_kd_trs));

    private void DeleteStok(IStockLedgerScopeKey scope, string fs_kd_trs)
        => _stokDal.Delete(fs_kd_trs);

    private sealed record BukuDeleteKey(string fs_kd_trs) : Itb_buku_key;

    private static int CountSyncIdentityKeys(IStockLedgerScopeKey scope)
    {
        using var conn = new System.Data.SqlClient.SqlConnection(
            ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        return conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1)
            FROM BILRG_StokSourceIdempotency
            WHERE BrgId = @BrgId
              AND ReceiptSourceId = @ReceiptSourceId
              AND IdempotencyKey LIKE 'SYNC|%'
            """,
            new { scope.BrgId, scope.ReceiptSourceId });
    }
}

using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.InventoryContext.MutasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Bilreg.Test.InventoryContext.StockLedgerFeature;
using Dapper;
using FluentAssertions;
using Nuna.Lib.AutoNumberHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

[Collection(StockLedgerSqlCollection.Name)]
public class MutasiBrgMovementServiceTest
{
    private readonly MutasiBrgMovementService _sut = new();

    private static string NextTestStokId()
        => $"TS{Guid.NewGuid():N}"[..10].ToUpperInvariant();

    private static SqlConnection CreateConnection()
    {
        var connStr = ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value);
        var conn = new SqlConnection(connStr);
        conn.Open();
        return conn;
    }

    private static void InsertStok(
        SqlConnection conn,
        SqlTransaction trans,
        string trsId,
        string brgId,
        string layananId,
        string batchNo,
        string expiredDate,
        decimal qty,
        decimal hpp = 100m)
    {
        const string sql = """
            INSERT INTO tb_stok(
                fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                fn_qty, fn_qty_in, fn_hpp,
                fd_tgl_do, fs_jam_do,
                fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi,
                fs_kd_satuan)
            VALUES(
                @fs_kd_trs, @fs_kd_barang, @fs_kd_layanan,
                @fs_kd_po, @fs_kd_do, @fd_tgl_ed, @fs_no_batch,
                @fn_qty, @fn_qty_in, @fn_hpp,
                @fd_tgl_do, @fs_jam_do,
                @fs_kd_mutasi, @fd_tgl_mutasi, @fs_jam_mutasi,
                @fs_kd_satuan)
            """;
        var dp = new DynamicParameters();
        dp.Add("@fs_kd_trs", trsId);
        dp.Add("@fs_kd_barang", brgId);
        dp.Add("@fs_kd_layanan", layananId);
        dp.Add("@fs_kd_po", "PO01");
        dp.Add("@fs_kd_do", "DO01");
        dp.Add("@fd_tgl_ed", expiredDate);
        dp.Add("@fs_no_batch", batchNo);
        dp.Add("@fn_qty", qty);
        dp.Add("@fn_qty_in", qty);
        dp.Add("@fn_hpp", hpp);
        dp.Add("@fd_tgl_do", "2025-01-01");
        dp.Add("@fs_jam_do", "08:00:00");
        dp.Add("@fs_kd_mutasi", "INIT");
        dp.Add("@fd_tgl_mutasi", "2025-01-01");
        dp.Add("@fs_jam_mutasi", "08:00:00");
        dp.Add("@fs_kd_satuan", "TAB");
        conn.Execute(sql, dp, transaction: trans);
    }

    [Fact]
    public void DeductStock_Normal_ShouldDeductUsingFefoOrder_AndRecordLedgerWithoutTruncation()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trs1 = NextTestStokId();
            var trs2 = NextTestStokId();
            InsertStok(conn, trans, trs1, brgId, layananAsal, "BATCH-A", "2025-02-01", 5m, 100m);
            InsertStok(conn, trans, trs2, brgId, layananAsal, "BATCH-B", "2025-06-01", 10m, 120m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            mutasiId.Length.Should().Be(10);

            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "user1");

            var item = MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Test"),
                string.Empty,
                string.Empty,
                8m,
                new SatuanType("TAB", "Tablet"));
            mutasi.AddItem(item);

            _sut.DeductStock(mutasi, conn, trans);

            var stock1 = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trs1 }, trans);
            var stock2 = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trs2 }, trans);

            stock1.Should().Be(0m);
            stock2.Should().Be(7m);

            var bukuRows = conn.Query<(string batch, decimal qtyOut, string jenis, string kdMutasi)>(
                """
                SELECT fs_no_batch AS batch, fn_stok_out AS qtyOut, fs_kd_jenis_mutasi AS jenis, fs_kd_mutasi AS kdMutasi
                FROM tb_buku
                WHERE fs_kd_mutasi = @MutasiId AND fs_kd_jenis_mutasi = 'MT_OUT'
                ORDER BY fs_no_batch
                """,
                new { MutasiId = mutasiId }, trans).ToList();

            bukuRows.Should().HaveCount(2);
            bukuRows[0].batch.Trim().Should().Be("BATCH-A");
            bukuRows[0].qtyOut.Should().Be(5m);
            bukuRows[0].kdMutasi.Trim().Should().Be(mutasiId);

            bukuRows[1].batch.Trim().Should().Be("BATCH-B");
            bukuRows[1].qtyOut.Should().Be(3m);
            bukuRows[1].kdMutasi.Trim().Should().Be(mutasiId);

            // Assert TRANS stock in tb_stok
            var transitStockRows = conn.Query<(string batch, decimal qty, decimal qtyIn, decimal hpp, string kdMutasi)>(
                """
                SELECT fs_no_batch AS batch, fn_qty AS qty, fn_qty_in AS qtyIn, fn_hpp AS hpp, fs_kd_mutasi AS kdMutasi
                FROM tb_stok
                WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId
                ORDER BY fs_no_batch
                """,
                new { TransitId = InventoryLocationIds.LogisticsTransitLayananId, BrgId = brgId }, trans).ToList();

            transitStockRows.Should().HaveCount(2);
            transitStockRows[0].batch.Trim().Should().Be("BATCH-A");
            transitStockRows[0].qty.Should().Be(5m);
            transitStockRows[0].qtyIn.Should().Be(5m);
            transitStockRows[0].hpp.Should().Be(100m);
            transitStockRows[0].kdMutasi.Trim().Should().Be(mutasiId);

            transitStockRows[1].batch.Trim().Should().Be("BATCH-B");
            transitStockRows[1].qty.Should().Be(3m);
            transitStockRows[1].qtyIn.Should().Be(3m);
            transitStockRows[1].hpp.Should().Be(120m);
            transitStockRows[1].kdMutasi.Trim().Should().Be(mutasiId);

            // Assert TRANS receipt ledger in tb_buku
            var transitBukuRows = conn.Query<(string batch, decimal qtyIn, decimal qtyOut, string jenis, string kdMutasi, string layanan)>(
                """
                SELECT fs_no_batch AS batch, fn_stok_in AS qtyIn, fn_stok_out AS qtyOut, fs_kd_jenis_mutasi AS jenis, fs_kd_mutasi AS kdMutasi, fs_kd_layanan AS layanan
                FROM tb_buku
                WHERE fs_kd_mutasi = @MutasiId AND fs_kd_jenis_mutasi = 'MT_IN'
                ORDER BY fs_no_batch
                """,
                new { MutasiId = mutasiId }, trans).ToList();

            transitBukuRows.Should().HaveCount(2);
            transitBukuRows[0].batch.Trim().Should().Be("BATCH-A");
            transitBukuRows[0].qtyIn.Should().Be(5m);
            transitBukuRows[0].qtyOut.Should().Be(0m);
            transitBukuRows[0].layanan.Trim().Should().Be(InventoryLocationIds.LogisticsTransitLayananId);
            transitBukuRows[0].kdMutasi.Trim().Should().Be(mutasiId);

            transitBukuRows[1].batch.Trim().Should().Be("BATCH-B");
            transitBukuRows[1].qtyIn.Should().Be(3m);
            transitBukuRows[1].qtyOut.Should().Be(0m);
            transitBukuRows[1].layanan.Trim().Should().Be(InventoryLocationIds.LogisticsTransitLayananId);
            transitBukuRows[1].kdMutasi.Trim().Should().Be(mutasiId);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void DeductStock_WhenTransitRowAlreadyExists_ShouldUpdateExistingTransitRow()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";
            var transitId = InventoryLocationIds.LogisticsTransitLayananId;

            var trsSource = NextTestStokId();
            InsertStok(conn, trans, trsSource, brgId, layananAsal, "BATCH-EX", "2025-08-01", 10m, 150m);

            // Pre-seed an existing stock row at TRANS with identical batch/ED/HPP/DO/PO
            var trsTransit = NextTestStokId();
            InsertStok(conn, trans, trsTransit, brgId, transitId, "BATCH-EX", "2025-08-01", 2m, 150m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "user1");

            var item = MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Test"),
                "BATCH-EX",
                "2025-08-01",
                4m,
                new SatuanType("TAB", "Tablet"));
            mutasi.AddItem(item);

            _sut.DeductStock(mutasi, conn, trans);

            // Source stock deducted: 10 - 4 = 6
            var sourceQty = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trsSource }, trans);
            sourceQty.Should().Be(6m);

            // Existing TRANS stock updated: 2 + 4 = 6 on the exact same row (no duplicate)
            var transitRows = conn.Query<(string trsId, decimal qty)>(
                "SELECT fs_kd_trs AS trsId, fn_qty AS qty FROM tb_stok WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId",
                new { TransitId = transitId, BrgId = brgId }, trans).ToList();

            transitRows.Should().HaveCount(1);
            transitRows[0].trsId.Trim().Should().Be(trsTransit);
            transitRows[0].qty.Should().Be(6m);

            // Buku entries recorded for both legs
            var outBuku = conn.QuerySingle<decimal>(
                "SELECT fn_stok_out FROM tb_buku WHERE fs_kd_mutasi = @MutasiId AND fs_kd_jenis_mutasi = 'MT_OUT'",
                new { MutasiId = mutasiId }, trans);
            outBuku.Should().Be(4m);

            var inBuku = conn.QuerySingle<decimal>(
                "SELECT fn_stok_in FROM tb_buku WHERE fs_kd_mutasi = @MutasiId AND fs_kd_jenis_mutasi = 'MT_IN'",
                new { MutasiId = mutasiId }, trans);
            inBuku.Should().Be(4m);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void DeductStock_WithSpecificBatch_ShouldOnlyDeductFromMatchingBatch()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trs1 = NextTestStokId();
            var trs2 = NextTestStokId();
            InsertStok(conn, trans, trs1, brgId, layananAsal, "BATCH-A", "2025-01-01", 10m);
            InsertStok(conn, trans, trs2, brgId, layananAsal, "BATCH-B", "2025-06-01", 10m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "user1");

            var item = MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Test"),
                "BATCH-B",
                "2025-06-01",
                4m,
                new SatuanType("TAB", "Tablet"));
            mutasi.AddItem(item);

            _sut.DeductStock(mutasi, conn, trans);

            var stock1 = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trs1 }, trans);
            var stock2 = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trs2 }, trans);

            stock1.Should().Be(10m);
            stock2.Should().Be(6m);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void DeductStock_InsufficientStock_ShouldThrowInvalidOperationException()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trs1 = NextTestStokId();
            InsertStok(conn, trans, trs1, brgId, layananAsal, "BATCH-A", "2025-01-01", 5m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "user1");

            var item = MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Test"),
                string.Empty,
                string.Empty,
                10m,
                new SatuanType("TAB", "Tablet"));
            mutasi.AddItem(item);

            var act = () => _sut.DeductStock(mutasi, conn, trans);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Stok tidak mencukupi*");
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void RestoreStock_AfterDeduction_ShouldRestoreStockAndRecordReversalLedgerWithoutTruncation()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trs1 = NextTestStokId();
            InsertStok(conn, trans, trs1, brgId, layananAsal, "BATCH-A", "2025-01-01", 10m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "user1");

            var item = MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Test"),
                "BATCH-A",
                "2025-01-01",
                4m,
                new SatuanType("TAB", "Tablet"));
            mutasi.AddItem(item);

            _sut.DeductStock(mutasi, conn, trans);

            var stockAfterDeduct = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trs1 }, trans);
            stockAfterDeduct.Should().Be(6m);

            var transitStockAfterDeduct = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId",
                new { TransitId = InventoryLocationIds.LogisticsTransitLayananId, BrgId = brgId }, trans);
            transitStockAfterDeduct.Should().Be(4m);

            mutasi.Dispatch("issuer1");
            mutasi.Cancel("canceler1");

            _sut.RestoreStock(mutasi, conn, trans);

            // 1. Source stock restored
            var stockAfterRestore = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trs1 }, trans);
            stockAfterRestore.Should().Be(10m);

            // 2. TRANS stock reduced back to 0
            var transitStockAfterRestore = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId",
                new { TransitId = InventoryLocationIds.LogisticsTransitLayananId, BrgId = brgId }, trans);
            transitStockAfterRestore.Should().Be(0m);

            // 3. Dual-leg reversal entries in tb_buku
            var revRows = conn.Query<(string layanan, decimal qtyIn, decimal qtyOut, string jenis, string kdMutasi)>(
                """
                SELECT fs_kd_layanan AS layanan, fn_stok_in AS qtyIn, fn_stok_out AS qtyOut, fs_kd_jenis_mutasi AS jenis, fs_kd_mutasi AS kdMutasi
                FROM tb_buku
                WHERE fs_kd_mutasi = @MutasiId AND fs_kd_jenis_mutasi IN ('MT_IN_V', 'MT_OUT_V')
                ORDER BY fs_kd_layanan
                """,
                new { MutasiId = mutasiId }, trans).ToList();

            revRows.Should().HaveCount(2);

            var transRev = revRows.Single(x => x.layanan.Trim() == InventoryLocationIds.LogisticsTransitLayananId);
            transRev.qtyIn.Should().Be(0m);
            transRev.qtyOut.Should().Be(4m);
            transRev.jenis.Trim().Should().Be("MT_IN_V");
            transRev.kdMutasi.Trim().Should().Be(mutasiId);

            var sourceRev = revRows.Single(x => x.layanan.Trim() == layananAsal);
            sourceRev.qtyIn.Should().Be(4m);
            sourceRev.qtyOut.Should().Be(0m);
            sourceRev.jenis.Trim().Should().Be("MT_OUT_V");
            sourceRev.kdMutasi.Trim().Should().Be(mutasiId);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void RestoreStock_WithoutPriorLedger_ShouldRestoreDirectlyFromItems()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trs1 = NextTestStokId();
            InsertStok(conn, trans, trs1, brgId, layananAsal, "BATCH-Z", "2025-01-01", 5m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "user1");

            var item = MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Test"),
                "BATCH-Z",
                "2025-01-01",
                3m,
                new SatuanType("TAB", "Tablet"));
            mutasi.AddItem(item);

            _sut.RestoreStock(mutasi, conn, trans);

            var stock = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trs1 }, trans);
            stock.Should().Be(8m);

            var revRows = conn.Query<(string layanan, decimal qtyIn, decimal qtyOut, string jenis, string kdMutasi)>(
                """
                SELECT fs_kd_layanan AS layanan, fn_stok_in AS qtyIn, fn_stok_out AS qtyOut, fs_kd_jenis_mutasi AS jenis, fs_kd_mutasi AS kdMutasi
                FROM tb_buku
                WHERE fs_kd_mutasi = @MutasiId AND fs_kd_jenis_mutasi IN ('MT_IN_V', 'MT_OUT_V')
                ORDER BY fs_kd_layanan
                """,
                new { MutasiId = mutasiId }, trans).ToList();

            revRows.Should().HaveCount(2);

            var transRev = revRows.Single(x => x.layanan.Trim() == InventoryLocationIds.LogisticsTransitLayananId);
            transRev.qtyIn.Should().Be(0m);
            transRev.qtyOut.Should().Be(3m);
            transRev.jenis.Trim().Should().Be("MT_IN_V");
            transRev.kdMutasi.Trim().Should().Be(mutasiId);

            var sourceRev = revRows.Single(x => x.layanan.Trim() == layananAsal);
            sourceRev.qtyIn.Should().Be(3m);
            sourceRev.qtyOut.Should().Be(0m);
            sourceRev.jenis.Trim().Should().Be("MT_OUT_V");
            sourceRev.kdMutasi.Trim().Should().Be(mutasiId);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void GuardClauses_NullParameters_ShouldThrow()
    {
        FluentActions.Invoking(() => _sut.DeductStock(null!, null!, null!))
            .Should().Throw<ArgumentNullException>();

        FluentActions.Invoking(() => _sut.RestoreStock(null!, null!, null!))
            .Should().Throw<ArgumentNullException>();
    }
}

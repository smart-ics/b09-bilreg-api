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
public class TerimaMutasiMovementServiceTest
{
    private readonly MutasiBrgMovementService _dispatchMovement = new();
    private readonly TerimaMutasiMovementService _sut = new();

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
    public void SettleReceivedStock_FullReceipt_ShouldMoveFromTransToTujuanAndRecordLedger()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trsAsal = NunaId.NewLegacyCompact("ST");
            InsertStok(conn, trans, trsAsal, brgId, layananAsal, "BATCH-A", "2025-05-01", 10m, 150m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "sender1");
            mutasi.AddItem(MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Test"),
                "BATCH-A",
                "2025-05-01",
                6m,
                new SatuanType("TAB", "Tablet")));

            _dispatchMovement.DeductStock(mutasi, conn, trans);

            var terima = TerimaMutasiModel.CreateConfirmed(
                mutasiId,
                string.Empty,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                "receiver1",
                [
                    TerimaMutasiItemModel.CreateConfirmed(
                        1,
                        new BrgReff(brgId, "Barang Test"),
                        "BATCH-A",
                        "2025-05-01",
                        6m,
                        6m,
                        0m,
                        new SatuanType("TAB", "Tablet"))
                ]);

            _sut.SettleReceivedStock(terima, conn, trans);

            var transitQty = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId",
                new { TransitId = InventoryLocationIds.LogisticsTransitLayananId, BrgId = brgId }, trans);
            transitQty.Should().Be(0m);

            var tujuanRow = conn.QuerySingle<(decimal qty, decimal hpp, string batch, string po, string doNo)>(
                """
                SELECT fn_qty AS qty, fn_hpp AS hpp, fs_no_batch AS batch, fs_kd_po AS po, fs_kd_do AS doNo
                FROM tb_stok
                WHERE fs_kd_layanan = @TujuanId AND fs_kd_barang = @BrgId
                """,
                new { TujuanId = layananTujuan, BrgId = brgId }, trans);
            tujuanRow.qty.Should().Be(6m);
            tujuanRow.hpp.Should().Be(150m);
            tujuanRow.batch.Trim().Should().Be("BATCH-A");
            tujuanRow.po.Trim().Should().Be("PO01");
            tujuanRow.doNo.Trim().Should().Be("DO01");

            var bukuRows = conn.Query<(string layanan, decimal qtyIn, decimal qtyOut, string jenis)>(
                """
                SELECT fs_kd_layanan AS layanan, fn_stok_in AS qtyIn, fn_stok_out AS qtyOut, fs_kd_jenis_mutasi AS jenis
                FROM tb_buku
                WHERE fs_kd_mutasi = @TerimaMutasiId
                """,
                new { TerimaMutasiId = terima.TerimaMutasiId }, trans).ToList();

            bukuRows.Should().HaveCount(2);
            bukuRows.Should().ContainSingle(x =>
                x.layanan.Trim() == InventoryLocationIds.LogisticsTransitLayananId
                && x.jenis.Trim() == "MUTASI_OUT"
                && x.qtyOut == 6m
                && x.qtyIn == 0m);
            bukuRows.Should().ContainSingle(x =>
                x.layanan.Trim() == layananTujuan
                && x.jenis.Trim() == "MUTASI_IN"
                && x.qtyIn == 6m
                && x.qtyOut == 0m);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void SettleReceivedStock_WithDiscrepancy_ShouldAddOnlyGoodQtyToTujuanAndRecordSelisihOutFromTrans()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trsAsal = NunaId.NewLegacyCompact("ST");
            InsertStok(conn, trans, trsAsal, brgId, layananAsal, "BATCH-D", "2025-07-01", 10m, 200m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "sender1");
            mutasi.AddItem(MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Rusak"),
                "BATCH-D",
                "2025-07-01",
                10m,
                new SatuanType("TAB", "Tablet")));

            _dispatchMovement.DeductStock(mutasi, conn, trans);

            var terima = TerimaMutasiModel.CreateConfirmed(
                mutasiId,
                string.Empty,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                "receiver1",
                [
                    TerimaMutasiItemModel.CreateConfirmed(
                        1,
                        new BrgReff(brgId, "Barang Rusak"),
                        "BATCH-D",
                        "2025-07-01",
                        10m,
                        7m,
                        3m,
                        new SatuanType("TAB", "Tablet"),
                        "3 tablet pecah")
                ]);

            _sut.SettleReceivedStock(terima, conn, trans);

            var transitQty = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId",
                new { TransitId = InventoryLocationIds.LogisticsTransitLayananId, BrgId = brgId }, trans);
            transitQty.Should().Be(0m);

            var tujuanQty = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_layanan = @TujuanId AND fs_kd_barang = @BrgId",
                new { TujuanId = layananTujuan, BrgId = brgId }, trans);
            tujuanQty.Should().Be(7m);

            var bukuRows = conn.Query<(string layanan, decimal qtyIn, decimal qtyOut, string jenis)>(
                """
                SELECT fs_kd_layanan AS layanan, fn_stok_in AS qtyIn, fn_stok_out AS qtyOut, fs_kd_jenis_mutasi AS jenis
                FROM tb_buku
                WHERE fs_kd_mutasi = @TerimaMutasiId
                """,
                new { TerimaMutasiId = terima.TerimaMutasiId }, trans).ToList();

            bukuRows.Should().HaveCount(3);
            bukuRows.Should().ContainSingle(x =>
                x.layanan.Trim() == InventoryLocationIds.LogisticsTransitLayananId
                && x.jenis.Trim() == "MUTASI_OUT"
                && x.qtyOut == 7m);
            bukuRows.Should().ContainSingle(x =>
                x.layanan.Trim() == layananTujuan
                && x.jenis.Trim() == "MUTASI_IN"
                && x.qtyIn == 7m);
            bukuRows.Should().ContainSingle(x =>
                x.layanan.Trim() == InventoryLocationIds.LogisticsTransitLayananId
                && x.jenis.Trim() == "MUTASI_SELISIH"
                && x.qtyOut == 3m
                && x.qtyIn == 0m);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void SettleReceivedStock_MultiBatch_ShouldSettleInFefoOrderWithoutIdCollision()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            InsertStok(conn, trans, NunaId.NewLegacyCompact("ST"), brgId, layananAsal, "BATCH-1", "2025-02-01", 5m, 100m);
            InsertStok(conn, trans, NunaId.NewLegacyCompact("ST"), brgId, layananAsal, "BATCH-2", "2025-06-01", 10m, 120m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "sender1");
            mutasi.AddItem(MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Multi"),
                string.Empty,
                string.Empty,
                8m,
                new SatuanType("TAB", "Tablet")));

            _dispatchMovement.DeductStock(mutasi, conn, trans);

            var terima = TerimaMutasiModel.CreateConfirmed(
                mutasiId,
                string.Empty,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                "receiver1",
                [
                    TerimaMutasiItemModel.CreateConfirmed(
                        1,
                        new BrgReff(brgId, "Barang Multi"),
                        string.Empty,
                        string.Empty,
                        8m,
                        7m,
                        1m,
                        new SatuanType("TAB", "Tablet"),
                        "1 rusak")
                ]);

            _sut.SettleReceivedStock(terima, conn, trans);

            var transitTotal = conn.QuerySingle<decimal>(
                "SELECT SUM(fn_qty) FROM tb_stok WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId",
                new { TransitId = InventoryLocationIds.LogisticsTransitLayananId, BrgId = brgId }, trans);
            transitTotal.Should().Be(0m);

            var tujuanBatches = conn.Query<(string batch, decimal qty, decimal hpp)>(
                """
                SELECT fs_no_batch AS batch, fn_qty AS qty, fn_hpp AS hpp
                FROM tb_stok
                WHERE fs_kd_layanan = @TujuanId AND fs_kd_barang = @BrgId
                ORDER BY fd_tgl_ed ASC
                """,
                new { TujuanId = layananTujuan, BrgId = brgId }, trans).ToList();

            tujuanBatches.Should().HaveCount(2);
            tujuanBatches[0].batch.Trim().Should().Be("BATCH-1");
            tujuanBatches[0].qty.Should().Be(5m);
            tujuanBatches[0].hpp.Should().Be(100m);

            tujuanBatches[1].batch.Trim().Should().Be("BATCH-2");
            tujuanBatches[1].qty.Should().Be(2m);
            tujuanBatches[1].hpp.Should().Be(120m);

            var selisihQty = conn.QuerySingle<decimal>(
                """
                SELECT fn_stok_out
                FROM tb_buku
                WHERE fs_kd_mutasi = @TerimaMutasiId AND fs_kd_jenis_mutasi = 'MUTASI_SELISIH'
                """,
                new { TerimaMutasiId = terima.TerimaMutasiId }, trans);
            selisihQty.Should().Be(1m);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void RestoreRejectedStock_TotalRejection_ShouldRestoreToAsalAndClearTrans()
    {
        using var conn = CreateConnection();
        using var trans = conn.BeginTransaction();
        try
        {
            var brgId = "TST_" + Guid.NewGuid().ToString("N")[..8];
            var layananAsal = "TS01";
            var layananTujuan = "TS02";

            var trsAsal = NunaId.NewLegacyCompact("ST");
            InsertStok(conn, trans, trsAsal, brgId, layananAsal, "BATCH-R", "2025-04-01", 10m, 175m);

            var mutasiId = NunaId.NewLegacy("MT", 'A');
            var mutasi = MutasiBrgModel.Create(
                mutasiId,
                DateTime.Now,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                userId: "sender1");
            mutasi.AddItem(MutasiBrgItemModel.Create(
                1,
                new BrgReff(brgId, "Barang Reject"),
                "BATCH-R",
                "2025-04-01",
                4m,
                new SatuanType("TAB", "Tablet")));

            _dispatchMovement.DeductStock(mutasi, conn, trans);

            var terimaRejected = TerimaMutasiModel.CreateRejected(
                mutasiId,
                string.Empty,
                new LayananReff(layananAsal, "Asal"),
                new LayananReff(layananTujuan, "Tujuan"),
                "receiver1",
                "Kemasan segel rusak semua",
                mutasi.ListItem);

            _sut.RestoreRejectedStock(terimaRejected, conn, trans);

            var asalQty = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_trs = @Id", new { Id = trsAsal }, trans);
            asalQty.Should().Be(10m);

            var transitQty = conn.QuerySingle<decimal>(
                "SELECT fn_qty FROM tb_stok WHERE fs_kd_layanan = @TransitId AND fs_kd_barang = @BrgId",
                new { TransitId = InventoryLocationIds.LogisticsTransitLayananId, BrgId = brgId }, trans);
            transitQty.Should().Be(0m);

            var tujuanCount = conn.QuerySingle<int>(
                "SELECT COUNT(*) FROM tb_stok WHERE fs_kd_layanan = @TujuanId AND fs_kd_barang = @BrgId",
                new { TujuanId = layananTujuan, BrgId = brgId }, trans);
            tujuanCount.Should().Be(0);

            var revRows = conn.Query<(string layanan, decimal qtyIn, decimal qtyOut, string jenis)>(
                """
                SELECT fs_kd_layanan AS layanan, fn_stok_in AS qtyIn, fn_stok_out AS qtyOut, fs_kd_jenis_mutasi AS jenis
                FROM tb_buku
                WHERE fs_kd_mutasi = @TerimaMutasiId AND fs_kd_jenis_mutasi = 'MUTASI_REV'
                """,
                new { TerimaMutasiId = terimaRejected.TerimaMutasiId }, trans).ToList();

            revRows.Should().HaveCount(2);
            revRows.Should().ContainSingle(x =>
                x.layanan.Trim() == InventoryLocationIds.LogisticsTransitLayananId
                && x.qtyOut == 4m
                && x.qtyIn == 0m);
            revRows.Should().ContainSingle(x =>
                x.layanan.Trim() == layananAsal
                && x.qtyIn == 4m
                && x.qtyOut == 0m);
        }
        finally
        {
            trans.Rollback();
        }
    }

    [Fact]
    public void GuardClauses_NullParameters_ShouldThrow()
    {
        FluentActions.Invoking(() => _sut.SettleReceivedStock(null!, null!, null!))
            .Should().Throw<ArgumentNullException>();

        FluentActions.Invoking(() => _sut.RestoreRejectedStock(null!, null!, null!))
            .Should().Throw<ArgumentNullException>();
    }
}

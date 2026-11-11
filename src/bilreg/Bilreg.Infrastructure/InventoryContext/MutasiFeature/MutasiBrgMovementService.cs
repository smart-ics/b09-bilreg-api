using System.Data;
using System.Data.SqlClient;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Dapper;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public class MutasiBrgMovementService : IMutasiBrgMovementService
{
    public MutasiBrgMovementService()
    {
    }

    public void DeductStock(MutasiBrgModel mutasi, SqlConnection conn, SqlTransaction trans)
    {
        Guard.Against.Null(mutasi, nameof(mutasi));
        Guard.Against.Null(conn, nameof(conn));
        Guard.Against.Null(trans, nameof(trans));

        if (mutasi.ListItem.Count == 0)
            return;

        var mutasiId = mutasi.MutasiBrgId;
        var layananId = mutasi.LayananAsal.LayananId;
        var tglMutasi = mutasi.MutasiBrgDate.ToString("yyyy-MM-dd");
        var jamMutasi = mutasi.MutasiBrgDate.ToString("HH:mm:ss");
        var tglJamMutasi = mutasi.MutasiBrgDate.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var item in mutasi.ListItem)
        {
            var brgId = item.Brg.BrgId;
            var qtyNeeded = item.QtyKirim;
            if (qtyNeeded <= 0)
                continue;

            var batchFilter = item.BatchNo?.Trim() ?? string.Empty;

            const string selectStokSql = """
                SELECT fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                       fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                       fn_qty, fn_qty_in, fn_hpp,
                       fd_tgl_do, fs_jam_do,
                       fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi,
                       fs_kd_satuan
                FROM tb_stok
                WHERE fs_kd_barang = @BrgId
                  AND fs_kd_layanan = @LayananId
                  AND fn_qty > 0
                  AND (@BatchFilter = '' OR fs_no_batch = @BatchFilter)
                ORDER BY fd_tgl_ed ASC, fs_kd_trs ASC
                """;

            var stockRows = conn.Query<StokRow>(
                selectStokSql,
                new { BrgId = brgId, LayananId = layananId, BatchFilter = batchFilter },
                transaction: trans
            ).ToList();

            var totalAvailable = stockRows.Sum(x => x.fn_qty);
            if (totalAvailable < qtyNeeded)
            {
                throw new InvalidOperationException(
                    $"Stok tidak mencukupi untuk barang '{brgId}' di unit '{layananId}'. Tersedia: {totalAvailable}, Dibutuhkan: {qtyNeeded}");
            }

            var remaining = qtyNeeded;
            foreach (var row in stockRows)
            {
                if (remaining <= 0)
                    break;

                var take = Math.Min(remaining, row.fn_qty);
                var newQty = row.fn_qty - take;

                const string updateStokSql = """
                    UPDATE tb_stok
                    SET fn_qty = @NewQty
                    WHERE fs_kd_trs = @TrsId
                    """;
                conn.Execute(updateStokSql, new { NewQty = newQty, TrsId = row.fs_kd_trs }, transaction: trans);

                var bukuId = NunaId.NewLegacyCompact("BK");
                const string insertBukuSql = """
                    INSERT INTO tb_buku(
                        fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                        fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                        fn_stok_in, fn_stok_out, fn_hpp,
                        fs_kd_mutasi, fd_tgl_jam_mutasi, fd_tgl_mutasi, fs_jam_mutasi,
                        fs_kd_jenis_mutasi, fs_kd_satuan)
                    VALUES(
                        @fs_kd_trs, @fs_kd_barang, @fs_kd_layanan,
                        @fs_kd_po, @fs_kd_do, @fd_tgl_ed, @fs_no_batch,
                        @fn_stok_in, @fn_stok_out, @fn_hpp,
                        @fs_kd_mutasi, @fd_tgl_jam_mutasi, @fd_tgl_mutasi, @fs_jam_mutasi,
                        @fs_kd_jenis_mutasi, @fs_kd_satuan)
                    """;

                var bukuDp = new DynamicParameters();
                bukuDp.AddParam("@fs_kd_trs", SafeSubstring(bukuId, 10), SqlDbType.VarChar);
                bukuDp.AddParam("@fs_kd_barang", SafeSubstring(row.fs_kd_barang, 13), SqlDbType.VarChar);
                bukuDp.AddParam("@fs_kd_layanan", SafeSubstring(row.fs_kd_layanan, 5), SqlDbType.VarChar);
                bukuDp.AddParam("@fs_kd_po", SafeSubstring(row.fs_kd_po, 10), SqlDbType.VarChar);
                bukuDp.AddParam("@fs_kd_do", SafeSubstring(row.fs_kd_do, 10), SqlDbType.VarChar);
                bukuDp.AddParam("@fd_tgl_ed", SafeSubstring(row.fd_tgl_ed, 10), SqlDbType.VarChar);
                bukuDp.AddParam("@fs_no_batch", SafeSubstring(row.fs_no_batch, 15), SqlDbType.VarChar);
                bukuDp.AddParam("@fn_stok_in", 0m, SqlDbType.Decimal);
                bukuDp.AddParam("@fn_stok_out", take, SqlDbType.Decimal);
                bukuDp.AddParam("@fn_hpp", row.fn_hpp, SqlDbType.Decimal);
                bukuDp.AddParam("@fs_kd_mutasi", mutasiId, SqlDbType.VarChar);
                bukuDp.AddParam("@fd_tgl_jam_mutasi", tglJamMutasi, SqlDbType.VarChar);
                bukuDp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
                bukuDp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
                bukuDp.AddParam("@fs_kd_jenis_mutasi", "MUTASI_OUT", SqlDbType.VarChar);
                bukuDp.AddParam("@fs_kd_satuan", SafeSubstring(string.IsNullOrWhiteSpace(row.fs_kd_satuan) ? item.Satuan.SatuanId : row.fs_kd_satuan, 3), SqlDbType.VarChar);

                conn.Execute(insertBukuSql, bukuDp, transaction: trans);

                remaining -= take;
            }
        }
    }

    public void RestoreStock(MutasiBrgModel mutasi, SqlConnection conn, SqlTransaction trans)
    {
        Guard.Against.Null(mutasi, nameof(mutasi));
        Guard.Against.Null(conn, nameof(conn));
        Guard.Against.Null(trans, nameof(trans));

        if (mutasi.ListItem.Count == 0)
            return;

        var mutasiId = mutasi.MutasiBrgId;
        var layananId = mutasi.LayananAsal.LayananId;
        var now = DateTime.Now;
        var tglMutasi = now.ToString("yyyy-MM-dd");
        var jamMutasi = now.ToString("HH:mm:ss");
        var tglJamMutasi = now.ToString("yyyy-MM-dd HH:mm:ss");

        const string selectBukuOutSql = """
            SELECT fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                   fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                   fn_stok_out, fn_hpp, fs_kd_satuan
            FROM tb_buku
            WHERE fs_kd_mutasi = @MutasiId
              AND fs_kd_jenis_mutasi = 'MUTASI_OUT'
              AND fs_kd_layanan = @LayananId
            """;

        var ledgerOutList = conn.Query<BukuOutRow>(
            selectBukuOutSql,
            new { MutasiId = mutasiId, LayananId = layananId },
            transaction: trans
        ).ToList();

        if (ledgerOutList.Count > 0)
        {
            foreach (var ledger in ledgerOutList)
            {
                var qtyToRestore = ledger.fn_stok_out;
                if (qtyToRestore <= 0) continue;

                RestoreSingleBatch(conn, trans, ledger.fs_kd_barang, layananId,
                    ledger.fs_no_batch, ledger.fd_tgl_ed, qtyToRestore, ledger.fn_hpp,
                    ledger.fs_kd_po, ledger.fs_kd_do, ledger.fs_kd_satuan,
                    mutasiId, tglMutasi, jamMutasi, tglJamMutasi);
            }
        }
        else
        {
            foreach (var item in mutasi.ListItem)
            {
                var brgId = item.Brg.BrgId;
                var qtyToRestore = item.QtyKirim;
                if (qtyToRestore <= 0) continue;

                var batchNo = item.BatchNo?.Trim() ?? string.Empty;
                var expiredDate = string.IsNullOrWhiteSpace(item.ExpiredDate) ? "3000-01-01" : item.ExpiredDate.Trim();

                RestoreSingleBatch(conn, trans, brgId, layananId,
                    batchNo, expiredDate, qtyToRestore, 0m,
                    string.Empty, string.Empty, item.Satuan?.SatuanId ?? string.Empty,
                    mutasiId, tglMutasi, jamMutasi, tglJamMutasi);
            }
        }
    }

    private static void RestoreSingleBatch(
        SqlConnection conn,
        SqlTransaction trans,
        string brgId,
        string layananId,
        string batchNo,
        string expiredDate,
        decimal qtyToRestore,
        decimal hpp,
        string po,
        string doNo,
        string satuanId,
        string mutasiId,
        string tglMutasi,
        string jamMutasi,
        string tglJamMutasi)
    {
        const string findStokSql = """
            SELECT TOP 1 fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                   fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                   fn_qty, fn_qty_in, fn_hpp,
                   fd_tgl_do, fs_jam_do,
                   fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi,
                   fs_kd_satuan
            FROM tb_stok
            WHERE fs_kd_barang = @BrgId
              AND fs_kd_layanan = @LayananId
              AND (@BatchNo = '' OR fs_no_batch = @BatchNo)
            ORDER BY fs_kd_trs DESC
            """;

        var existingStok = conn.QueryFirstOrDefault<StokRow>(
            findStokSql,
            new { BrgId = brgId, LayananId = layananId, BatchNo = batchNo },
            transaction: trans
        );

        if (existingStok is not null)
        {
            const string updateStokSql = """
                UPDATE tb_stok
                SET fn_qty = fn_qty + @Qty
                WHERE fs_kd_trs = @TrsId
                """;
            conn.Execute(updateStokSql, new { Qty = qtyToRestore, TrsId = existingStok.fs_kd_trs }, transaction: trans);

            if (string.IsNullOrWhiteSpace(po)) po = existingStok.fs_kd_po;
            if (string.IsNullOrWhiteSpace(doNo)) doNo = existingStok.fs_kd_do;
            if (string.IsNullOrWhiteSpace(expiredDate)) expiredDate = existingStok.fd_tgl_ed;
            if (string.IsNullOrWhiteSpace(batchNo)) batchNo = existingStok.fs_no_batch;
            if (hpp <= 0) hpp = existingStok.fn_hpp;
            if (string.IsNullOrWhiteSpace(satuanId)) satuanId = existingStok.fs_kd_satuan;
        }
        else
        {
            var newStokId = NunaId.NewLegacyCompact("ST");
            const string insertStokSql = """
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

            var stokDp = new DynamicParameters();
            stokDp.AddParam("@fs_kd_trs", SafeSubstring(newStokId, 10), SqlDbType.VarChar);
            stokDp.AddParam("@fs_kd_barang", SafeSubstring(brgId, 13), SqlDbType.VarChar);
            stokDp.AddParam("@fs_kd_layanan", SafeSubstring(layananId, 5), SqlDbType.VarChar);
            stokDp.AddParam("@fs_kd_po", SafeSubstring(po, 10), SqlDbType.VarChar);
            stokDp.AddParam("@fs_kd_do", SafeSubstring(doNo, 10), SqlDbType.VarChar);
            stokDp.AddParam("@fd_tgl_ed", SafeSubstring(string.IsNullOrWhiteSpace(expiredDate) ? "3000-01-01" : expiredDate, 10), SqlDbType.VarChar);
            stokDp.AddParam("@fs_no_batch", SafeSubstring(batchNo, 15), SqlDbType.VarChar);
            stokDp.AddParam("@fn_qty", qtyToRestore, SqlDbType.Decimal);
            stokDp.AddParam("@fn_qty_in", qtyToRestore, SqlDbType.Decimal);
            stokDp.AddParam("@fn_hpp", hpp, SqlDbType.Decimal);
            stokDp.AddParam("@fd_tgl_do", tglMutasi, SqlDbType.VarChar);
            stokDp.AddParam("@fs_jam_do", jamMutasi, SqlDbType.VarChar);
            stokDp.AddParam("@fs_kd_mutasi", mutasiId, SqlDbType.VarChar);
            stokDp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
            stokDp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
            stokDp.AddParam("@fs_kd_satuan", SafeSubstring(satuanId, 3), SqlDbType.VarChar);

            conn.Execute(insertStokSql, stokDp, transaction: trans);
        }

        var bukuRevId = NunaId.NewLegacyCompact("BK");
        const string insertBukuRevSql = """
            INSERT INTO tb_buku(
                fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                fn_stok_in, fn_stok_out, fn_hpp,
                fs_kd_mutasi, fd_tgl_jam_mutasi, fd_tgl_mutasi, fs_jam_mutasi,
                fs_kd_jenis_mutasi, fs_kd_satuan)
            VALUES(
                @fs_kd_trs, @fs_kd_barang, @fs_kd_layanan,
                @fs_kd_po, @fs_kd_do, @fd_tgl_ed, @fs_no_batch,
                @fn_stok_in, @fn_stok_out, @fn_hpp,
                @fs_kd_mutasi, @fd_tgl_jam_mutasi, @fd_tgl_mutasi, @fs_jam_mutasi,
                @fs_kd_jenis_mutasi, @fs_kd_satuan)
            """;

        var revDp = new DynamicParameters();
        revDp.AddParam("@fs_kd_trs", SafeSubstring(bukuRevId, 10), SqlDbType.VarChar);
        revDp.AddParam("@fs_kd_barang", SafeSubstring(brgId, 13), SqlDbType.VarChar);
        revDp.AddParam("@fs_kd_layanan", SafeSubstring(layananId, 5), SqlDbType.VarChar);
        revDp.AddParam("@fs_kd_po", SafeSubstring(po, 10), SqlDbType.VarChar);
        revDp.AddParam("@fs_kd_do", SafeSubstring(doNo, 10), SqlDbType.VarChar);
        revDp.AddParam("@fd_tgl_ed", SafeSubstring(string.IsNullOrWhiteSpace(expiredDate) ? "3000-01-01" : expiredDate, 10), SqlDbType.VarChar);
        revDp.AddParam("@fs_no_batch", SafeSubstring(batchNo, 15), SqlDbType.VarChar);
        revDp.AddParam("@fn_stok_in", qtyToRestore, SqlDbType.Decimal);
        revDp.AddParam("@fn_stok_out", 0m, SqlDbType.Decimal);
        revDp.AddParam("@fn_hpp", hpp, SqlDbType.Decimal);
        revDp.AddParam("@fs_kd_mutasi", mutasiId, SqlDbType.VarChar);
        revDp.AddParam("@fd_tgl_jam_mutasi", tglJamMutasi, SqlDbType.VarChar);
        revDp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
        revDp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
        revDp.AddParam("@fs_kd_jenis_mutasi", "MUTASI_REV", SqlDbType.VarChar);
        revDp.AddParam("@fs_kd_satuan", SafeSubstring(satuanId, 3), SqlDbType.VarChar);

        conn.Execute(insertBukuRevSql, revDp, transaction: trans);
    }

    private static string SafeSubstring(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private sealed record StokRow(
        string fs_kd_trs,
        string fs_kd_barang,
        string fs_kd_layanan,
        string fs_kd_po,
        string fs_kd_do,
        string fd_tgl_ed,
        string fs_no_batch,
        decimal fn_qty,
        decimal fn_qty_in,
        decimal fn_hpp,
        string fd_tgl_do,
        string fs_jam_do,
        string fs_kd_mutasi,
        string fd_tgl_mutasi,
        string fs_jam_mutasi,
        string fs_kd_satuan);

    private sealed record BukuOutRow(
        string fs_kd_trs,
        string fs_kd_barang,
        string fs_kd_layanan,
        string fs_kd_po,
        string fs_kd_do,
        string fd_tgl_ed,
        string fs_no_batch,
        decimal fn_stok_out,
        decimal fn_hpp,
        string fs_kd_satuan);
}

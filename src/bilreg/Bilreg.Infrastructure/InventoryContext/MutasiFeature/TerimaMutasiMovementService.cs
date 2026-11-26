using System.Data;
using System.Data.SqlClient;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Domain.InventoryContext;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Dapper;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public class TerimaMutasiMovementService : ITerimaMutasiMovementService
{
    public void SettleReceivedStock(TerimaMutasiModel terimaMutasi, SqlConnection conn, SqlTransaction trans)
    {
        Guard.Against.Null(terimaMutasi, nameof(terimaMutasi));
        Guard.Against.Null(conn, nameof(conn));
        Guard.Against.Null(trans, nameof(trans));

        if (terimaMutasi.ListItem.Count == 0)
            return;

        var transitLayananId = InventoryLocationIds.LogisticsTransitLayananId;
        var tujuanLayananId = terimaMutasi.LayananTujuan.LayananId;
        var terimaMutasiId = terimaMutasi.TerimaMutasiId;
        var mutasiBrgId = terimaMutasi.MutasiBrgId;
        var tglMutasi = terimaMutasi.TerimaMutasiDate.ToString("yyyy-MM-dd");
        var jamMutasi = terimaMutasi.TerimaMutasiDate.ToString("HH:mm:ss");
        var tglJamMutasi = terimaMutasi.TerimaMutasiDate.ToString("yyyy-MM-dd HH:mm:ss");

        var layers = QueryTransitBukuInLayers(conn, trans, mutasiBrgId, transitLayananId);

        foreach (var item in terimaMutasi.ListItem)
        {
            var brgId = item.Brg.BrgId.Trim();
            var batchFilter = item.BatchNo?.Trim() ?? string.Empty;
            var matchingLayers = layers
                .Where(x => x.RemainingQty > 0
                    && string.Equals(x.BrgId, brgId, StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrEmpty(batchFilter)
                        || string.Equals(x.BatchNo, batchFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var available = matchingLayers.Sum(x => x.RemainingQty);
            if (available < item.QtyTerima)
            {
                throw new InvalidOperationException(
                    $"Stok transit pada tb_buku tidak mencukupi untuk barang '{brgId}' mutasi '{mutasiBrgId}'. Tersedia: {available}, Dibutuhkan: {item.QtyTerima}");
            }

            var remaining = item.QtyTerima;

            foreach (var layer in matchingLayers)
            {
                if (remaining <= 0)
                    break;

                var take = Math.Min(remaining, layer.RemainingQty);
                layer.RemainingQty -= take;
                remaining -= take;

                DeductTransitStokExact(conn, trans, transitLayananId, layer, take);

                var satuanId = string.IsNullOrWhiteSpace(layer.SatuanId)
                    ? item.Satuan.SatuanId
                    : layer.SatuanId;

                InsertBuku(conn, trans, MutasiLegacyIdGenerator.NextId("BK"), layer.BrgId, transitLayananId,
                    layer.PoNo, layer.DoNo, layer.ExpiredDate, layer.BatchNo,
                    stokIn: 0m, stokOut: take, layer.Hpp,
                    terimaMutasiId, tglJamMutasi, tglMutasi, jamMutasi,
                    "MT_OUT", satuanId);

                UpsertStok(conn, trans, tujuanLayananId, layer.BrgId,
                    layer.BatchNo, layer.ExpiredDate, take, layer.Hpp,
                    layer.PoNo, layer.DoNo, satuanId,
                    terimaMutasiId, tglMutasi, jamMutasi);

                InsertBuku(conn, trans, MutasiLegacyIdGenerator.NextId("BK"), layer.BrgId, tujuanLayananId,
                    layer.PoNo, layer.DoNo, layer.ExpiredDate, layer.BatchNo,
                    stokIn: take, stokOut: 0m, layer.Hpp,
                    terimaMutasiId, tglJamMutasi, tglMutasi, jamMutasi,
                    "MT_IN", satuanId);
            }
        }
    }

    private static List<TransitBukuLayer> QueryTransitBukuInLayers(
        SqlConnection conn,
        SqlTransaction trans,
        string mutasiBrgId,
        string transitLayananId)
    {
        const string sql = """
            SELECT fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                   fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                   fn_stok_in, fn_hpp, fs_kd_satuan
            FROM tb_buku
            WHERE fs_kd_mutasi = @MutasiBrgId
              AND fs_kd_layanan = @TransitLayananId
              AND fs_kd_jenis_mutasi = 'MT_IN'
            ORDER BY fd_tgl_ed ASC, fs_kd_trs ASC
            """;

        var rows = conn.Query<BukuInRow>(
            sql,
            new { MutasiBrgId = mutasiBrgId, TransitLayananId = transitLayananId },
            transaction: trans
        ).ToList();

        return rows
            .Where(r => r.fn_stok_in > 0)
            .Select(r => new TransitBukuLayer(
                r.fs_kd_barang?.Trim() ?? string.Empty,
                r.fs_kd_po?.Trim() ?? string.Empty,
                r.fs_kd_do?.Trim() ?? string.Empty,
                string.IsNullOrWhiteSpace(r.fd_tgl_ed) ? "3000-01-01" : r.fd_tgl_ed.Trim(),
                r.fs_no_batch?.Trim() ?? string.Empty,
                r.fn_stok_in,
                r.fn_hpp,
                r.fs_kd_satuan?.Trim() ?? string.Empty))
            .ToList();
    }

    private static void DeductTransitStokExact(
        SqlConnection conn,
        SqlTransaction trans,
        string transitLayananId,
        TransitBukuLayer layer,
        decimal qtyToDeduct)
    {
        const string findSql = """
            SELECT TOP 1 fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                   fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                   fn_qty, fn_qty_in, fn_hpp, fs_kd_satuan
            FROM tb_stok
            WHERE fs_kd_layanan = @TransitLayananId
              AND fs_kd_barang = @BrgId
              AND fs_no_batch = @BatchNo
              AND fd_tgl_ed = @ExpiredDate
              AND fn_hpp = @Hpp
              AND fs_kd_do = @DoNo
              AND fs_kd_po = @PoNo
            ORDER BY fs_kd_trs DESC
            """;

        var existing = conn.QueryFirstOrDefault<StokRow>(
            findSql,
            new
            {
                TransitLayananId = transitLayananId,
                BrgId = layer.BrgId,
                BatchNo = layer.BatchNo,
                ExpiredDate = layer.ExpiredDate,
                Hpp = layer.Hpp,
                DoNo = layer.DoNo,
                PoNo = layer.PoNo
            },
            transaction: trans);

        if (existing is null)
        {
            DeductTransitStokFallback(conn, trans, transitLayananId, layer, qtyToDeduct);
            return;
        }

        const string updateSql = """
            UPDATE tb_stok
            SET fn_qty = fn_qty - @Qty
            WHERE fs_kd_trs = @TrsId
            """;
        conn.Execute(updateSql, new { Qty = qtyToDeduct, TrsId = existing.fs_kd_trs }, transaction: trans);
    }

    private static TransitBukuLayer DeductTransitStokFallback(
        SqlConnection conn,
        SqlTransaction trans,
        string transitLayananId,
        TransitBukuLayer layer,
        decimal qtyToDeduct)
    {
        const string findSql = """
            SELECT TOP 1 fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                   fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                   fn_qty, fn_qty_in, fn_hpp, fs_kd_satuan
            FROM tb_stok
            WHERE fs_kd_layanan = @TransitLayananId
              AND fs_kd_barang = @BrgId
              AND (@BatchNo = '' OR fs_no_batch = @BatchNo)
            ORDER BY fs_kd_trs DESC
            """;

        var existing = conn.QueryFirstOrDefault<StokRow>(
            findSql,
            new { TransitLayananId = transitLayananId, BrgId = layer.BrgId, BatchNo = layer.BatchNo },
            transaction: trans);

        if (existing is null)
            return layer;

        const string updateSql = """
            UPDATE tb_stok
            SET fn_qty = fn_qty - @Qty
            WHERE fs_kd_trs = @TrsId
            """;
        conn.Execute(updateSql, new { Qty = qtyToDeduct, TrsId = existing.fs_kd_trs }, transaction: trans);

        return new TransitBukuLayer(
            layer.BrgId,
            string.IsNullOrWhiteSpace(layer.PoNo) ? existing.fs_kd_po?.Trim() ?? string.Empty : layer.PoNo,
            string.IsNullOrWhiteSpace(layer.DoNo) ? existing.fs_kd_do?.Trim() ?? string.Empty : layer.DoNo,
            string.IsNullOrWhiteSpace(layer.ExpiredDate) ? existing.fd_tgl_ed?.Trim() ?? "3000-01-01" : layer.ExpiredDate,
            string.IsNullOrWhiteSpace(layer.BatchNo) ? existing.fs_no_batch?.Trim() ?? string.Empty : layer.BatchNo,
            layer.RemainingQty,
            layer.Hpp <= 0 ? existing.fn_hpp : layer.Hpp,
            string.IsNullOrWhiteSpace(layer.SatuanId) ? existing.fs_kd_satuan?.Trim() ?? string.Empty : layer.SatuanId);
    }

    private static void UpsertStok(
        SqlConnection conn,
        SqlTransaction trans,
        string layananId,
        string brgId,
        string batchNo,
        string expiredDate,
        decimal qty,
        decimal hpp,
        string poNo,
        string doNo,
        string satuanId,
        string mutasiId,
        string tglMutasi,
        string jamMutasi)
    {
        const string findSql = """
            SELECT TOP 1 fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                   fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                   fn_qty, fn_qty_in, fn_hpp, fs_kd_satuan
            FROM tb_stok
            WHERE fs_kd_layanan = @LayananId
              AND fs_kd_barang = @BrgId
              AND fs_no_batch = @BatchNo
              AND fd_tgl_ed = @ExpiredDate
              AND fn_hpp = @Hpp
              AND fs_kd_do = @DoNo
              AND fs_kd_po = @PoNo
            ORDER BY fs_kd_trs DESC
            """;

        var existing = conn.QueryFirstOrDefault<StokRow>(
            findSql,
            new
            {
                LayananId = SafeSubstring(layananId, 5),
                BrgId = SafeSubstring(brgId, 13),
                BatchNo = SafeSubstring(batchNo, 15),
                ExpiredDate = SafeSubstring(expiredDate, 10),
                Hpp = hpp,
                DoNo = SafeSubstring(doNo, 10),
                PoNo = SafeSubstring(poNo, 10)
            },
            transaction: trans);

        if (existing is not null)
        {
            const string updateSql = """
                UPDATE tb_stok
                SET fn_qty = fn_qty + @Qty
                WHERE fs_kd_trs = @TrsId
                """;
            conn.Execute(updateSql, new { Qty = qty, TrsId = existing.fs_kd_trs }, transaction: trans);
            return;
        }

        const string insertSql = """
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
        dp.AddParam("@fs_kd_trs", MutasiLegacyIdGenerator.NextId("ST"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_barang", SafeSubstring(brgId, 13), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", SafeSubstring(layananId, 5), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", SafeSubstring(poNo, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", SafeSubstring(doNo, 10), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", SafeSubstring(string.IsNullOrWhiteSpace(expiredDate) ? "3000-01-01" : expiredDate, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", SafeSubstring(batchNo, 15), SqlDbType.VarChar);
        dp.AddParam("@fn_qty", qty, SqlDbType.Decimal);
        dp.AddParam("@fn_qty_in", qty, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp", hpp, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_do", tglMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_do", jamMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_mutasi", SafeSubstring(mutasiId, 10), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_satuan", SafeSubstring(satuanId, 3), SqlDbType.VarChar);

        conn.Execute(insertSql, dp, transaction: trans);
    }

    private static void InsertBuku(
        SqlConnection conn,
        SqlTransaction trans,
        string bukuId,
        string brgId,
        string layananId,
        string poNo,
        string doNo,
        string expiredDate,
        string batchNo,
        decimal stokIn,
        decimal stokOut,
        decimal hpp,
        string mutasiId,
        string tglJamMutasi,
        string tglMutasi,
        string jamMutasi,
        string jenisMutasi,
        string satuanId)
    {
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

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", SafeSubstring(bukuId, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_barang", SafeSubstring(brgId, 13), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", SafeSubstring(layananId, 5), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", SafeSubstring(poNo, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", SafeSubstring(doNo, 10), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", SafeSubstring(string.IsNullOrWhiteSpace(expiredDate) ? "3000-01-01" : expiredDate, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", SafeSubstring(batchNo, 15), SqlDbType.VarChar);
        dp.AddParam("@fn_stok_in", stokIn, SqlDbType.Decimal);
        dp.AddParam("@fn_stok_out", stokOut, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp", hpp, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_mutasi", SafeSubstring(mutasiId, 10), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jam_mutasi", tglJamMutasi, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_mutasi", jenisMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_satuan", SafeSubstring(satuanId, 3), SqlDbType.VarChar);

        conn.Execute(insertBukuSql, dp, transaction: trans);
    }

    private static string SafeSubstring(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private sealed class TransitBukuLayer
    {
        public TransitBukuLayer(
            string brgId,
            string poNo,
            string doNo,
            string expiredDate,
            string batchNo,
            decimal remainingQty,
            decimal hpp,
            string satuanId)
        {
            BrgId = brgId;
            PoNo = poNo;
            DoNo = doNo;
            ExpiredDate = expiredDate;
            BatchNo = batchNo;
            RemainingQty = remainingQty;
            Hpp = hpp;
            SatuanId = satuanId;
        }

        public string BrgId { get; }
        public string PoNo { get; }
        public string DoNo { get; }
        public string ExpiredDate { get; }
        public string BatchNo { get; }
        public decimal RemainingQty { get; set; }
        public decimal Hpp { get; }
        public string SatuanId { get; }
    }

    private sealed record BukuInRow(
        string fs_kd_trs,
        string fs_kd_barang,
        string fs_kd_layanan,
        string fs_kd_po,
        string fs_kd_do,
        string fd_tgl_ed,
        string fs_no_batch,
        decimal fn_stok_in,
        decimal fn_hpp,
        string fs_kd_satuan);

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
        string fs_kd_satuan);
}

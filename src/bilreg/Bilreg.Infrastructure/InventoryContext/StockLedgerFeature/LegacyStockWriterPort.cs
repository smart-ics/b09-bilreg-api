using System.Data;
using System.Data.SqlClient;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// Dual-write writer for legacy <c>tb_buku</c> / <c>tb_stok</c>.
/// Void = reverse buku insert only — never DELETE buku.
/// </summary>
public class LegacyStockWriterPort : ILegacyStockWriterPort
{
    private readonly DatabaseOptions _opt;

    public LegacyStockWriterPort(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public LegacyInboundWriteResult InsertInbound(LegacyInboundWriteRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.MovementKindString);
        if (request.Qty <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Qty), "Inbound qty must be positive.");

        var bukuId = ResolveLegacyId(request.LegacyBukuId, "BK");
        var stokId = ResolveLegacyId(request.LegacyStokId, "ST");
        var po = request.PoReffId ?? string.Empty;
        var noBatch = request.NoBatch ?? string.Empty;
        var satuan = string.IsNullOrWhiteSpace(request.SatuanId) ? string.Empty : request.SatuanId;
        var tglEd = LegacyStockDateHelper.ToLegacyEdString(request.TglEd);
        var tglMutasi = LegacyStockDateHelper.FormatDate(request.TglMutasi);
        var jamMutasi = LegacyStockDateHelper.FormatTime(request.TglMutasi);
        var tglJamMutasi = LegacyStockDateHelper.FormatDateTime(request.TglMutasi);
        var tglDo = LegacyStockDateHelper.FormatDate(request.TglMasuk);
        var jamDo = LegacyStockDateHelper.FormatTime(request.TglMasuk);

        const string insertBuku = """
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

        const string insertStok = """
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

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        var bukuDp = new DynamicParameters();
        bukuDp.AddParam("@fs_kd_trs", bukuId, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_kd_barang", request.BrgId, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_kd_layanan", request.LayananId, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_kd_po", po, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_kd_do", request.BrgMasukReffId, SqlDbType.VarChar);
        bukuDp.AddParam("@fd_tgl_ed", tglEd, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_no_batch", noBatch, SqlDbType.VarChar);
        bukuDp.AddParam("@fn_stok_in", request.Qty, SqlDbType.Decimal);
        bukuDp.AddParam("@fn_stok_out", 0m, SqlDbType.Decimal);
        bukuDp.AddParam("@fn_hpp", request.Hpp, SqlDbType.Decimal);
        bukuDp.AddParam("@fs_kd_mutasi", request.TrsReffId, SqlDbType.VarChar);
        bukuDp.AddParam("@fd_tgl_jam_mutasi", tglJamMutasi, SqlDbType.VarChar);
        bukuDp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_kd_jenis_mutasi", request.MovementKindString, SqlDbType.VarChar);
        bukuDp.AddParam("@fs_kd_satuan", satuan, SqlDbType.VarChar);
        conn.Execute(insertBuku, bukuDp);

        var stokDp = new DynamicParameters();
        stokDp.AddParam("@fs_kd_trs", stokId, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_barang", request.BrgId, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_layanan", request.LayananId, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_po", po, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_do", request.BrgMasukReffId, SqlDbType.VarChar);
        stokDp.AddParam("@fd_tgl_ed", tglEd, SqlDbType.VarChar);
        stokDp.AddParam("@fs_no_batch", noBatch, SqlDbType.VarChar);
        stokDp.AddParam("@fn_qty", request.Qty, SqlDbType.Decimal);
        stokDp.AddParam("@fn_qty_in", request.Qty, SqlDbType.Decimal);
        stokDp.AddParam("@fn_hpp", request.Hpp, SqlDbType.Decimal);
        stokDp.AddParam("@fd_tgl_do", tglDo, SqlDbType.VarChar);
        stokDp.AddParam("@fs_jam_do", jamDo, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_mutasi", request.TrsReffId, SqlDbType.VarChar);
        stokDp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
        stokDp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_satuan", satuan, SqlDbType.VarChar);
        conn.Execute(insertStok, stokDp);

        return new LegacyInboundWriteResult(bukuId, stokId);
    }

    public LegacyOutboundWriteResult InsertOutboundBuku(LegacyOutboundBukuWriteRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.MovementKindString);
        if (request.QtyOut <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.QtyOut), "Outbound qty must be positive.");

        var bukuId = ResolveLegacyId(request.LegacyBukuId, "BK");
        InsertBukuRow(
            bukuId,
            request.BrgId,
            request.LayananId,
            request.PoReffId ?? string.Empty,
            request.BrgMasukReffId,
            request.TglEd,
            request.NoBatch ?? string.Empty,
            qtyIn: 0,
            qtyOut: request.QtyOut,
            request.Hpp,
            request.TrsReffId,
            request.TglMutasi,
            request.MovementKindString,
            request.SatuanId ?? string.Empty);

        return new LegacyOutboundWriteResult(bukuId);
    }

    public void DepleteStok(LegacyStokDepleteRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.LegacyStokId);
        if (request.QtyOut <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.QtyOut), "Deplete qty must be positive.");

        const string selectSql = """
            SELECT fn_qty
            FROM tb_stok
            WHERE fs_kd_trs = @fs_kd_trs
            """;
        var selectDp = new DynamicParameters();
        selectDp.AddParam("@fs_kd_trs", request.LegacyStokId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        var current = conn.ExecuteScalar<decimal?>(selectSql, selectDp);
        if (current is null)
            throw new InvalidOperationException($"Legacy stok '{request.LegacyStokId}' not found.");

        var remaining = current.Value - request.QtyOut;
        if (remaining < 0)
            throw new InvalidOperationException(
                $"Legacy stok '{request.LegacyStokId}' insufficient " +
                $"(have {current.Value}, deplete {request.QtyOut}).");

        if (remaining == 0)
        {
            // Legacy RemoveStok deletes depleted stok rows; never deletes buku.
            const string deleteSql = """
                DELETE FROM tb_stok
                WHERE fs_kd_trs = @fs_kd_trs
                """;
            conn.Execute(deleteSql, selectDp);
            return;
        }

        const string updateSql = """
            UPDATE tb_stok
            SET fn_qty = @fn_qty
            WHERE fs_kd_trs = @fs_kd_trs
            """;
        var updateDp = new DynamicParameters();
        updateDp.AddParam("@fs_kd_trs", request.LegacyStokId, SqlDbType.VarChar);
        updateDp.AddParam("@fn_qty", remaining, SqlDbType.Decimal);
        conn.Execute(updateSql, updateDp);
    }

    public LegacyReverseWriteResult InsertReverseBuku(LegacyReverseBukuWriteRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        Guard.Against.NullOrWhiteSpace(request.MovementKindString);
        if (request.QtyIn <= 0 && request.QtyOut <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Reverse qty must have one positive side.");
        if (request.QtyIn > 0 && request.QtyOut > 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Reverse qty must not set both sides.");

        var bukuId = ResolveLegacyId(request.LegacyBukuId, "BK");
        InsertBukuRow(
            bukuId,
            request.BrgId,
            request.LayananId,
            request.PoReffId ?? string.Empty,
            request.BrgMasukReffId,
            request.TglEd,
            request.NoBatch ?? string.Empty,
            request.QtyIn,
            request.QtyOut,
            request.Hpp,
            request.TrsReffId,
            request.TglMutasi,
            request.MovementKindString,
            request.SatuanId ?? string.Empty);

        return new LegacyReverseWriteResult(bukuId);
    }

    public LegacyStokRestoreResult RestoreStok(LegacyStokRestoreRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.PreferredLegacyStokId);
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.NullOrWhiteSpace(request.BrgMasukReffId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TrsReffId);
        if (request.QtyIn <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.QtyIn), "Restore qty must be positive.");

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        // Atomic increment: avoids SELECT-then-UPDATE race where deplete deletes the row between check and update.
        const string updateSql = """
            UPDATE tb_stok
            SET fn_qty = fn_qty + @fn_qty_in
            WHERE fs_kd_trs = @fs_kd_trs
            """;
        var updateDp = new DynamicParameters();
        updateDp.AddParam("@fs_kd_trs", request.PreferredLegacyStokId, SqlDbType.VarChar);
        updateDp.AddParam("@fn_qty_in", request.QtyIn, SqlDbType.Decimal);
        var rowsAffected = conn.Execute(updateSql, updateDp);

        if (rowsAffected == 1)
            return new LegacyStokRestoreResult(request.PreferredLegacyStokId, WasRecreated: false);

        // rowsAffected == 0: preferred row gone (deplete-to-zero or concurrent delete) → recreate.
        // Prefer Binding-chosen id (IfRecreate), else Preferred — never mint anonymous when Binding already chose.
        var stokId = ResolveLegacyId(
            request.LegacyStokIdIfRecreate ?? request.PreferredLegacyStokId,
            "ST");
        var po = request.PoReffId ?? string.Empty;
        var noBatch = request.NoBatch ?? string.Empty;
        var satuan = string.IsNullOrWhiteSpace(request.SatuanId) ? string.Empty : request.SatuanId;
        var tglEd = LegacyStockDateHelper.ToLegacyEdString(request.TglEd);
        var tglMutasi = LegacyStockDateHelper.FormatDate(request.TglMutasi);
        var jamMutasi = LegacyStockDateHelper.FormatTime(request.TglMutasi);
        var tglDo = LegacyStockDateHelper.FormatDate(request.TglMasuk);
        var jamDo = LegacyStockDateHelper.FormatTime(request.TglMasuk);

        const string insertStok = """
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
        stokDp.AddParam("@fs_kd_trs", stokId, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_barang", request.BrgId, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_layanan", request.LayananId, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_po", po, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_do", request.BrgMasukReffId, SqlDbType.VarChar);
        stokDp.AddParam("@fd_tgl_ed", tglEd, SqlDbType.VarChar);
        stokDp.AddParam("@fs_no_batch", noBatch, SqlDbType.VarChar);
        stokDp.AddParam("@fn_qty", request.QtyIn, SqlDbType.Decimal);
        stokDp.AddParam("@fn_qty_in", request.QtyIn, SqlDbType.Decimal);
        stokDp.AddParam("@fn_hpp", request.Hpp, SqlDbType.Decimal);
        stokDp.AddParam("@fd_tgl_do", tglDo, SqlDbType.VarChar);
        stokDp.AddParam("@fs_jam_do", jamDo, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_mutasi", request.TrsReffId, SqlDbType.VarChar);
        stokDp.AddParam("@fd_tgl_mutasi", tglMutasi, SqlDbType.VarChar);
        stokDp.AddParam("@fs_jam_mutasi", jamMutasi, SqlDbType.VarChar);
        stokDp.AddParam("@fs_kd_satuan", satuan, SqlDbType.VarChar);
        conn.Execute(insertStok, stokDp);

        return new LegacyStokRestoreResult(stokId, WasRecreated: true);
    }

    private void InsertBukuRow(
        string bukuId,
        string brgId,
        string layananId,
        string poReffId,
        string brgMasukReffId,
        DateTime tglEd,
        string noBatch,
        decimal qtyIn,
        decimal qtyOut,
        decimal hpp,
        string trsReffId,
        DateTime tglMutasi,
        string movementKindString,
        string satuanId)
    {
        const string insertBuku = """
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
        dp.AddParam("@fs_kd_trs", bukuId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_barang", brgId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", layananId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", poReffId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", brgMasukReffId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", LegacyStockDateHelper.ToLegacyEdString(tglEd), SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", noBatch, SqlDbType.VarChar);
        dp.AddParam("@fn_stok_in", qtyIn, SqlDbType.Decimal);
        dp.AddParam("@fn_stok_out", qtyOut, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp", hpp, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_mutasi", trsReffId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jam_mutasi", LegacyStockDateHelper.FormatDateTime(tglMutasi), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mutasi", LegacyStockDateHelper.FormatDate(tglMutasi), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_mutasi", LegacyStockDateHelper.FormatTime(tglMutasi), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_mutasi", movementKindString, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_satuan", satuanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(insertBuku, dp);
    }

    private static string ResolveLegacyId(string? preassigned, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(preassigned))
            return preassigned.Trim();

        return NunaId.NewLegacyCompact(prefix);
    }
}

using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S1 / G-11 — Live DM receipt post compatibility writer.
/// Inserts authoritative <c>tb_buku</c> then <c>tb_stok</c> rows enlisted in the caller's ambient transaction.
/// Receipt post only; void / other FO families remain fail-closed until later Phase 4 slices.
/// Does not transfer Stage B authority. Not registered in production DI.
/// </summary>
public sealed class LegacyCompatibilityWriterPort : ILegacyCompatibilityWriterPort
{
    private const string MutationKindDo = "DO";
    private const string PrefixBuku = "BK";
    private const string PrefixStok = "ST";
    private const string SentinelDate = "3000-01-01";
    private const string SentinelTime = "00:00:00";

    private readonly DatabaseOptions _opt;

    public LegacyCompatibilityWriterPort(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Apply(LegacyCompatibilityWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MovementKind != StockMovementKindEnum.Receipt)
        {
            throw new NotSupportedException(
                $"P4-S1 LegacyCompatibilityWriterPort supports Receipt post only; " +
                $"movement kind '{request.MovementKind}' is not supported.");
        }

        if (request.JournalEntries.Count != request.BalanceMutations.Count)
        {
            throw new InvalidOperationException(
                "DM receipt post requires JournalEntries and BalanceMutations to be paired 1:1 " +
                $"(journals={request.JournalEntries.Count}, balances={request.BalanceMutations.Count}).");
        }

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        for (var i = 0; i < request.BalanceMutations.Count; i++)
        {
            var balance = request.BalanceMutations[i];
            var journal = request.JournalEntries[i];
            ValidateReceiptLine(balance, journal, i);
            InsertBukuThenStok(conn, balance, journal);
        }
    }

    private static void ValidateReceiptLine(
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal,
        int index)
    {
        if (balance.Action != LegacyBalanceMutationActionEnum.Upsert)
        {
            throw new NotSupportedException(
                $"P4-S1 DM receipt post supports Upsert balance action only (line {index}); " +
                $"Delete/void is deferred to P4-S4.");
        }

        if (journal.IsVoid)
        {
            throw new NotSupportedException(
                $"P4-S1 DM receipt post does not support void journal entries (line {index}); " +
                $"void is deferred to P4-S4.");
        }

        if (string.IsNullOrWhiteSpace(balance.BrgId) || string.IsNullOrWhiteSpace(balance.ReceiptSourceId))
            throw new InvalidOperationException($"DM receipt balance line {index} requires BrgId and ReceiptSourceId.");

        if (string.IsNullOrWhiteSpace(journal.BrgId) || string.IsNullOrWhiteSpace(journal.ReceiptSourceId))
            throw new InvalidOperationException($"DM receipt journal line {index} requires BrgId and ReceiptSourceId.");

        if (!string.Equals(balance.BrgId.Trim(), journal.BrgId.Trim(), StringComparison.Ordinal) ||
            !string.Equals(balance.ReceiptSourceId.Trim(), journal.ReceiptSourceId.Trim(), StringComparison.Ordinal) ||
            !string.Equals(balance.LayananId.Trim(), journal.LayananId.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"DM receipt line {index} balance/journal identity mismatch (BrgId/ReceiptSourceId/LayananId).");
        }

        var mutationKind = string.IsNullOrWhiteSpace(journal.MutationKindId)
            ? MutationKindDo
            : journal.MutationKindId.Trim();
        if (!string.Equals(mutationKind, MutationKindDo, StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"P4-S1 DM receipt post requires MutationKindId '{MutationKindDo}' (line {index}); " +
                $"got '{mutationKind}'.");
        }

        if (journal.QuantityOut != 0m)
        {
            throw new InvalidOperationException(
                $"DM receipt journal line {index} must have QuantityOut = 0 (got {journal.QuantityOut}).");
        }

        if (journal.QuantityIn <= 0m || balance.Quantity <= 0m)
        {
            throw new InvalidOperationException(
                $"DM receipt line {index} requires positive inbound quantity.");
        }

        if (journal.QuantityIn != balance.Quantity)
        {
            throw new InvalidOperationException(
                $"DM receipt line {index} journal QuantityIn ({journal.QuantityIn}) " +
                $"must equal balance Quantity ({balance.Quantity}).");
        }
    }

    private static void InsertBukuThenStok(
        SqlConnection conn,
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal)
    {
        var mutationTime = journal.MutationTime;
        var mutasiDate = FormatDate(mutationTime);
        var mutasiTime = FormatTime(mutationTime);
        var mutasiCombined = FormatDateTime(mutationTime);
        var expiration = FormatExpiration(balance.ExpirationDate ?? journal.ExpirationDate);
        var batch = NullToEmpty(balance.Batch ?? journal.Batch);
        var po = NullToEmpty(balance.PurchaseOrderId);
        var satuan = NullToEmpty(balance.SmallestUnitId ?? journal.SmallestUnitId);
        var mutasiId = string.IsNullOrWhiteSpace(journal.MutationTransactionId)
            ? balance.ReceiptSourceId.Trim()
            : journal.MutationTransactionId.Trim();
        var doId = balance.ReceiptSourceId.Trim();
        var brgId = balance.BrgId.Trim();
        var layananId = balance.LayananId.Trim();
        var qty = balance.Quantity;
        var hpp = balance.UnitCost;

        // VB6 AddStok order: tb_buku INSERT then tb_stok INSERT. Always INSERT (never merge).
        var bukuId = NunaId.NewLegacyCompact(PrefixBuku);
        InsertBuku(conn, new BukuInsert(
            bukuId, brgId, layananId, po, doId, expiration, batch,
            qty, 0m, hpp, mutasiId, mutasiDate, mutasiTime, mutasiCombined, MutationKindDo, satuan));

        var stokId = NunaId.NewLegacyCompact(PrefixStok);
        InsertStok(conn, new StokInsert(
            stokId, brgId, layananId, po, doId, expiration, batch,
            qty, qty, hpp, mutasiId, mutasiDate, mutasiTime, satuan));
    }

    private static void InsertBuku(SqlConnection conn, BukuInsert row)
    {
        const string sql = """
            INSERT INTO tb_buku(
                fs_kd_trs, fs_kd_barang, fs_kd_layanan,
                fs_kd_po, fs_kd_do, fd_tgl_ed, fs_no_batch,
                fn_stok_in, fn_stok_out, fn_hpp,
                fs_kd_mutasi, fd_tgl_mutasi, fs_jam_mutasi, fd_tgl_jam_mutasi,
                fs_kd_jenis_mutasi, fs_kd_satuan)
            VALUES(
                @fs_kd_trs, @fs_kd_barang, @fs_kd_layanan,
                @fs_kd_po, @fs_kd_do, @fd_tgl_ed, @fs_no_batch,
                @fn_stok_in, @fn_stok_out, @fn_hpp,
                @fs_kd_mutasi, @fd_tgl_mutasi, @fs_jam_mutasi, @fd_tgl_jam_mutasi,
                @fs_kd_jenis_mutasi, @fs_kd_satuan)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", row.FsKdTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_barang", row.FsKdBarang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", row.FsKdLayanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", row.FsKdPo, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", row.FsKdDo, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", row.FdTglEd, SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", row.FsNoBatch, SqlDbType.VarChar);
        dp.AddParam("@fn_stok_in", row.FnStokIn, SqlDbType.Decimal);
        dp.AddParam("@fn_stok_out", row.FnStokOut, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp", row.FnHpp, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_mutasi", row.FsKdMutasi, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mutasi", row.FdTglMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_mutasi", row.FsJamMutasi, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jam_mutasi", row.FdTglJamMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_mutasi", row.FsKdJenisMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_satuan", row.FsKdSatuan, SqlDbType.VarChar);
        conn.Execute(sql, dp);
    }

    private static void InsertStok(SqlConnection conn, StokInsert row)
    {
        // VB6 parity for fd_tgl_do / fs_jam_do: omit FO receipt time — use schema defaults
        // (3000-01-01 / 00:00:00). LegacyStockReadPort maps those sentinels to ReceiptTime = null;
        // LastMutationTime comes from fd_tgl_mutasi / fs_jam_mutasi which we do populate.
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
        dp.AddParam("@fs_kd_trs", row.FsKdTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_barang", row.FsKdBarang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", row.FsKdLayanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_po", row.FsKdPo, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", row.FsKdDo, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_ed", row.FdTglEd, SqlDbType.VarChar);
        dp.AddParam("@fs_no_batch", row.FsNoBatch, SqlDbType.VarChar);
        dp.AddParam("@fn_qty", row.FnQty, SqlDbType.Decimal);
        dp.AddParam("@fn_qty_in", row.FnQtyIn, SqlDbType.Decimal);
        dp.AddParam("@fn_hpp", row.FnHpp, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_do", SentinelDate, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_do", SentinelTime, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_mutasi", row.FsKdMutasi, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_mutasi", row.FdTglMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_mutasi", row.FsJamMutasi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_satuan", row.FsKdSatuan, SqlDbType.VarChar);
        conn.Execute(sql, dp);
    }

    private static string FormatDate(DateTime value)
        => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatTime(DateTime value)
        => value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    private static string FormatDateTime(DateTime value)
        => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string FormatExpiration(DateOnly? value)
        => value is null ? SentinelDate : value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string NullToEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private sealed record BukuInsert(
        string FsKdTrs,
        string FsKdBarang,
        string FsKdLayanan,
        string FsKdPo,
        string FsKdDo,
        string FdTglEd,
        string FsNoBatch,
        decimal FnStokIn,
        decimal FnStokOut,
        decimal FnHpp,
        string FsKdMutasi,
        string FdTglMutasi,
        string FsJamMutasi,
        string FdTglJamMutasi,
        string FsKdJenisMutasi,
        string FsKdSatuan);

    private sealed record StokInsert(
        string FsKdTrs,
        string FsKdBarang,
        string FsKdLayanan,
        string FsKdPo,
        string FsKdDo,
        string FdTglEd,
        string FsNoBatch,
        decimal FnQty,
        decimal FnQtyIn,
        decimal FnHpp,
        string FsKdMutasi,
        string FdTglMutasi,
        string FsJamMutasi,
        string FsKdSatuan);
}

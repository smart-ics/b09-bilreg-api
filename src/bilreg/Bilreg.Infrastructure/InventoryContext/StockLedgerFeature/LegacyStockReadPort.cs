using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S1 / G-05 — Live read-only legacy reconstruction adapter.
/// Parameterized <c>tb_stok</c> / <c>tb_buku</c> reads by Item + Receipt Source across all Stock Locations.
/// Does not mutate legacy tables and does not encode runtime authority transfer.
/// </summary>
public sealed class LegacyStockReadPort : ILegacyStockReadPort
{
    private readonly DatabaseOptions _opt;

    public LegacyStockReadPort(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IReadOnlyList<LegacyStockBalanceType> ListCurrentBalances(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        const string sql = """
            SELECT
                aa.fs_kd_trs,
                aa.fs_kd_barang,
                aa.fs_kd_do,
                aa.fs_kd_layanan,
                aa.fn_qty,
                aa.fn_hpp,
                aa.fd_tgl_ed,
                aa.fs_no_batch,
                aa.fs_kd_po,
                aa.fd_tgl_do,
                aa.fs_jam_do,
                aa.fd_tgl_mutasi,
                aa.fs_jam_mutasi
            FROM tb_stok aa
            WHERE
                aa.fs_kd_barang = @BrgId
                AND aa.fs_kd_do = @ReceiptSourceId
            ORDER BY
                aa.fs_kd_layanan,
                aa.fs_kd_trs
            """;

        var dp = ScopeParams(scope);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<LegacyBalanceRow>(sql, dp) ?? [];
        return rows.Select(MapBalance).ToList();
    }

    public IReadOnlyList<LegacyStockJournalEntryType> ListJournalEntries(IStockLedgerScopeKey scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        const string sql = """
            SELECT
                aa.fs_kd_trs,
                aa.fs_kd_barang,
                aa.fs_kd_do,
                aa.fs_kd_layanan,
                aa.fn_stok_in,
                aa.fn_stok_out,
                aa.fn_hpp,
                aa.fd_tgl_ed,
                aa.fs_no_batch,
                aa.fs_kd_po,
                aa.fs_kd_jenis_mutasi,
                aa.fs_kd_mutasi,
                aa.fd_tgl_jam_mutasi,
                aa.fd_tgl_mutasi,
                aa.fs_jam_mutasi
            FROM tb_buku aa
            WHERE
                aa.fs_kd_barang = @BrgId
                AND aa.fs_kd_do = @ReceiptSourceId
            ORDER BY
                aa.fd_tgl_jam_mutasi,
                aa.fs_kd_trs,
                aa.fs_kd_layanan
            """;

        var dp = ScopeParams(scope);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<LegacyJournalRow>(sql, dp) ?? [];
        return rows.Select(MapJournal).ToList();
    }

    private static DynamicParameters ScopeParams(IStockLedgerScopeKey scope)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", scope.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", scope.ReceiptSourceId, SqlDbType.VarChar);
        return dp;
    }

    private static LegacyStockBalanceType MapBalance(LegacyBalanceRow row)
        => new(
            BrgId: row.fs_kd_barang ?? string.Empty,
            ReceiptSourceId: row.fs_kd_do ?? string.Empty,
            LayananId: row.fs_kd_layanan ?? string.Empty,
            Quantity: row.fn_qty,
            UnitCost: row.fn_hpp,
            ExpirationDate: ParseDateOnly(row.fd_tgl_ed),
            Batch: EmptyToNull(row.fs_no_batch),
            PurchaseOrderId: EmptyToNull(row.fs_kd_po),
            LegacyRowId: EmptyToNull(row.fs_kd_trs),
            ReceiptTime: ParseDateTimeParts(row.fd_tgl_do, row.fs_jam_do),
            LastMutationTime: ParseDateTimeParts(row.fd_tgl_mutasi, row.fs_jam_mutasi));

    private static LegacyStockJournalEntryType MapJournal(LegacyJournalRow row)
        => new(
            LegacyJournalId: row.fs_kd_trs ?? string.Empty,
            BrgId: row.fs_kd_barang ?? string.Empty,
            ReceiptSourceId: row.fs_kd_do ?? string.Empty,
            LayananId: row.fs_kd_layanan ?? string.Empty,
            QuantityIn: row.fn_stok_in,
            QuantityOut: row.fn_stok_out,
            UnitCost: row.fn_hpp,
            ExpirationDate: ParseDateOnly(row.fd_tgl_ed),
            Batch: EmptyToNull(row.fs_no_batch),
            MutationKindId: row.fs_kd_jenis_mutasi ?? string.Empty,
            MutationTransactionId: row.fs_kd_mutasi ?? string.Empty,
            MutationTime: ParseMutationTime(row),
            PurchaseOrderId: EmptyToNull(row.fs_kd_po));

    private static DateTime ParseMutationTime(LegacyJournalRow row)
    {
        var combined = ParseDateTimeCombined(row.fd_tgl_jam_mutasi);
        if (combined.HasValue)
            return combined.Value;

        var parts = ParseDateTimeParts(row.fd_tgl_mutasi, row.fs_jam_mutasi);
        return parts ?? DateTime.MinValue;
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("3000-01-01", StringComparison.Ordinal))
            return null;

        try
        {
            var dt = trimmed.ToDate(DateFormatEnum.YMD);
            if (dt.Year >= 2999)
                return null;
            return DateOnly.FromDateTime(dt);
        }
        catch
        {
            if (DateOnly.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dateOnly))
                return dateOnly;
            return null;
        }
    }

    private static DateTime? ParseDateTimeCombined(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("3000-01-01", StringComparison.Ordinal))
            return null;

        try
        {
            var dt = trimmed.ToDate(DateFormatEnum.YMD_HMS);
            return dt.Year >= 2999 ? null : dt;
        }
        catch
        {
            if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt.Year >= 2999 ? null : dt;
            return null;
        }
    }

    private static DateTime? ParseDateTimeParts(string? datePart, string? timePart)
    {
        if (string.IsNullOrWhiteSpace(datePart))
            return null;

        var date = datePart.Trim();
        if (date.StartsWith("3000-01-01", StringComparison.Ordinal))
            return null;

        var time = string.IsNullOrWhiteSpace(timePart) ? "00:00:00" : timePart.Trim();
        if (time.Length == 5)
            time += ":00";

        return ParseDateTimeCombined($"{date} {time}");
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record LegacyBalanceRow(
        string? fs_kd_trs,
        string? fs_kd_barang,
        string? fs_kd_do,
        string? fs_kd_layanan,
        decimal fn_qty,
        decimal fn_hpp,
        string? fd_tgl_ed,
        string? fs_no_batch,
        string? fs_kd_po,
        string? fd_tgl_do,
        string? fs_jam_do,
        string? fd_tgl_mutasi,
        string? fs_jam_mutasi);

    private sealed record LegacyJournalRow(
        string? fs_kd_trs,
        string? fs_kd_barang,
        string? fs_kd_do,
        string? fs_kd_layanan,
        decimal fn_stok_in,
        decimal fn_stok_out,
        decimal fn_hpp,
        string? fd_tgl_ed,
        string? fs_no_batch,
        string? fs_kd_po,
        string? fs_kd_jenis_mutasi,
        string? fs_kd_mutasi,
        string? fd_tgl_jam_mutasi,
        string? fd_tgl_mutasi,
        string? fs_jam_mutasi);
}

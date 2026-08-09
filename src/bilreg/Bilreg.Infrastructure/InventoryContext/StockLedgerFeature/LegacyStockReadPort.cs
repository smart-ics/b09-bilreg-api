using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// Scoped Item+DO reads of <c>tb_buku</c> / <c>tb_stok</c>. Does not reuse Layanan-scoped
/// <c>tb_buku_dal.ListData</c> (cross-DO unsafe for hydrate).
/// </summary>
public class LegacyStockReadPort : ILegacyStockReadPort
{
    private static readonly string SentinelDate = StockLedgerSentinel.EmptyDate.ToString("yyyy-MM-dd");
    private static readonly string SentinelDateTime = StockLedgerSentinel.EmptyDate.ToString("yyyy-MM-dd HH:mm:ss");

    private readonly DatabaseOptions _opt;

    public LegacyStockReadPort(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IReadOnlyList<LegacyStockJournalReadModel> ListJournals(string brgId, string brgMasukReffId)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs,
                aa.fs_kd_barang,
                aa.fs_kd_layanan,
                aa.fs_kd_po,
                aa.fs_kd_do,
                aa.fd_tgl_ed,
                aa.fs_no_batch,
                aa.fn_stok_in,
                aa.fn_stok_out,
                aa.fn_hpp,
                aa.fs_kd_mutasi,
                aa.fd_tgl_mutasi,
                aa.fs_jam_mutasi,
                aa.fd_tgl_jam_mutasi,
                aa.fs_kd_jenis_mutasi
            FROM tb_buku aa
            WHERE
                aa.fs_kd_barang = @fs_kd_barang
                AND aa.fs_kd_do = @fs_kd_do
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", brgId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", brgMasukReffId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<LegacyBukuRow>(sql, dp) ?? [];
        // Order after MapJournal so sort keys match ComposeMutasiDateTime (null/empty jam → 00:00:00).
        return rows
            .Select(MapJournal)
            .OrderBy(x => x.TglMutasi)
            .ThenBy(x => x.LegacyBukuId, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<LegacyStockBalanceReadModel> ListBalances(string brgId, string brgMasukReffId)
    {
        const string sql = """
            SELECT
                aa.fs_kd_trs,
                aa.fs_kd_barang,
                aa.fs_kd_layanan,
                aa.fs_kd_do,
                aa.fd_tgl_ed,
                aa.fs_no_batch,
                aa.fn_qty,
                aa.fn_hpp,
                aa.fd_tgl_do,
                aa.fs_jam_do
            FROM tb_stok aa
            WHERE
                aa.fs_kd_barang = @fs_kd_barang
                AND aa.fs_kd_do = @fs_kd_do
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", brgId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_do", brgMasukReffId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<LegacyStokRow>(sql, dp) ?? [];
        return rows.Select(MapBalance).ToList();
    }

    private static LegacyStockJournalReadModel MapJournal(LegacyBukuRow row) =>
        new(
            LegacyBukuId: row.fs_kd_trs ?? string.Empty,
            BrgId: row.fs_kd_barang ?? string.Empty,
            BrgMasukReffId: row.fs_kd_do ?? string.Empty,
            LayananId: row.fs_kd_layanan ?? string.Empty,
            PoReffId: row.fs_kd_po ?? string.Empty,
            NoBatch: row.fs_no_batch ?? string.Empty,
            QtyIn: row.fn_stok_in,
            QtyOut: row.fn_stok_out,
            Hpp: row.fn_hpp,
            MovementKindString: row.fs_kd_jenis_mutasi ?? string.Empty,
            TrsReffId: string.IsNullOrWhiteSpace(row.fs_kd_mutasi) ? row.fs_kd_trs ?? string.Empty : row.fs_kd_mutasi,
            TglMutasi: ComposeMutasiDateTime(row.fd_tgl_jam_mutasi, row.fd_tgl_mutasi, row.fs_jam_mutasi),
            TglEd: ParseLegacyDate(row.fd_tgl_ed));

    private static LegacyStockBalanceReadModel MapBalance(LegacyStokRow row) =>
        new(
            LegacyStokId: row.fs_kd_trs ?? string.Empty,
            BrgId: row.fs_kd_barang ?? string.Empty,
            BrgMasukReffId: row.fs_kd_do ?? string.Empty,
            LayananId: row.fs_kd_layanan ?? string.Empty,
            TglEd: ParseLegacyDate(row.fd_tgl_ed),
            NoBatch: row.fs_no_batch ?? string.Empty,
            QtySisa: row.fn_qty,
            Hpp: row.fn_hpp,
            TglMasuk: ComposeDateAndTime(row.fd_tgl_do, row.fs_jam_do));

    internal static DateTime ParseLegacyDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return StockLedgerSentinel.EmptyDate;

        var trimmed = value.Trim();
        if (trimmed.StartsWith(SentinelDate, StringComparison.Ordinal))
            return StockLedgerSentinel.EmptyDate;

        if (DateTime.TryParseExact(
                trimmed.Length >= 10 ? trimmed[..10] : trimmed,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            return date.Date;

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
            return fallback.Date;

        return StockLedgerSentinel.EmptyDate;
    }

    internal static DateTime ComposeMutasiDateTime(string? tglJam, string? tgl, string? jam)
    {
        if (!string.IsNullOrWhiteSpace(tglJam))
        {
            var trimmed = tglJam.Trim();
            if (!trimmed.StartsWith(SentinelDate, StringComparison.Ordinal) &&
                trimmed != SentinelDateTime &&
                DateTime.TryParseExact(
                    trimmed,
                    ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm"],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var fromCombo))
                return fromCombo;
        }

        return ComposeDateAndTime(tgl, jam);
    }

    internal static DateTime ComposeDateAndTime(string? tgl, string? jam)
    {
        if (string.IsNullOrWhiteSpace(tgl) || tgl.Trim().StartsWith(SentinelDate, StringComparison.Ordinal))
            return StockLedgerSentinel.EmptyDate;

        var datePart = tgl.Trim().Length >= 10 ? tgl.Trim()[..10] : tgl.Trim();
        var timePart = string.IsNullOrWhiteSpace(jam) ? "00:00:00" : jam.Trim();
        if (timePart.Length == 5)
            timePart += ":00";

        if (DateTime.TryParseExact(
                $"{datePart} {timePart}",
                ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var composed))
            return composed;

        return ParseLegacyDate(datePart);
    }

    // ReSharper disable InconsistentNaming
    private sealed class LegacyBukuRow
    {
        public string? fs_kd_trs { get; init; }
        public string? fs_kd_barang { get; init; }
        public string? fs_kd_layanan { get; init; }
        public string? fs_kd_po { get; init; }
        public string? fs_kd_do { get; init; }
        public string? fd_tgl_ed { get; init; }
        public string? fs_no_batch { get; init; }
        public decimal fn_stok_in { get; init; }
        public decimal fn_stok_out { get; init; }
        public decimal fn_hpp { get; init; }
        public string? fs_kd_mutasi { get; init; }
        public string? fd_tgl_mutasi { get; init; }
        public string? fs_jam_mutasi { get; init; }
        public string? fd_tgl_jam_mutasi { get; init; }
        public string? fs_kd_jenis_mutasi { get; init; }
    }

    private sealed class LegacyStokRow
    {
        public string? fs_kd_trs { get; init; }
        public string? fs_kd_barang { get; init; }
        public string? fs_kd_layanan { get; init; }
        public string? fs_kd_do { get; init; }
        public string? fd_tgl_ed { get; init; }
        public string? fs_no_batch { get; init; }
        public decimal fn_qty { get; init; }
        public decimal fn_hpp { get; init; }
        public string? fd_tgl_do { get; init; }
        public string? fs_jam_do { get; init; }
    }
    // ReSharper restore InconsistentNaming
}

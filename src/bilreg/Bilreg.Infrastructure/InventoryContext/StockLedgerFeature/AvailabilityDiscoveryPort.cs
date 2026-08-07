using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S7 / G-08 — Live provisional Availability Discovery against Legacy Stock Authority.
/// Reads surviving <c>tb_stok</c> rows by Item + Stock Location (+ optional Expiration Date),
/// returning Receipt Source candidates — not final FIFO allocation.
/// <para>
/// Does not consult Stock Ledger layers, does not implement Freshness Gate,
/// and does not mutate legacy tables.
/// </para>
/// </summary>
public sealed class AvailabilityDiscoveryPort : IAvailabilityDiscoveryPort
{
    private readonly DatabaseOptions _opt;

    public AvailabilityDiscoveryPort(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public AvailabilityDiscoveryResult Discover(
        IBrgKey item,
        ILayananKey stockLocation,
        DateOnly? expirationDateFilter = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(stockLocation);

        // Authoritative coexistence input is surviving tb_stok quantity at the requested
        // Item + Stock Location. Stock Ledger layers are intentionally not read.
        const string sql = """
            SELECT
                aa.fs_kd_trs,
                aa.fs_kd_barang,
                aa.fs_kd_do,
                aa.fs_kd_layanan,
                aa.fn_qty,
                aa.fd_tgl_ed,
                aa.fs_no_batch
            FROM tb_stok aa
            WHERE
                aa.fs_kd_barang = @BrgId
                AND aa.fs_kd_layanan = @LayananId
                AND aa.fn_qty > 0
            ORDER BY
                aa.fs_kd_do,
                aa.fd_tgl_ed,
                aa.fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", item.BrgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", stockLocation.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<LegacyAvailabilityRow>(sql, dp) ?? [];

        var candidates = new List<AvailabilityCandidateType>();
        foreach (var row in rows)
        {
            var expirationDate = ParseDateOnly(row.fd_tgl_ed);
            if (expirationDateFilter.HasValue && expirationDate != expirationDateFilter.Value)
                continue;

            var quantity = row.fn_qty;
            if (quantity <= 0m)
                continue;

            var receiptSourceId = row.fs_kd_do?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(receiptSourceId))
                continue;

            candidates.Add(new AvailabilityCandidateType(
                ReceiptSourceId: receiptSourceId,
                LayananId: row.fs_kd_layanan?.Trim() ?? stockLocation.LayananId,
                AvailableQuantity: quantity,
                ExpirationDate: expirationDate,
                Batch: EmptyToNull(row.fs_no_batch)));
        }

        if (candidates.Count == 0)
        {
            return new AvailabilityDiscoveryResult(
                AvailabilityDiscoveryOutcomeEnum.InsufficientAuthoritativeStock,
                Array.Empty<AvailabilityCandidateType>(),
                "No authoritative available quantity found in legacy stock for the requested Item and Stock Location"
                + (expirationDateFilter.HasValue
                    ? " with the requested Expiration Date."
                    : "."));
        }

        // StaleOrNotCurrent is reserved for Phase 3/5 Freshness Gate callers.
        // This adapter returns provisional candidates from legacy authority only.
        return new AvailabilityDiscoveryResult(
            AvailabilityDiscoveryOutcomeEnum.CandidatesFound,
            candidates,
            "Provisional Receipt Source candidates from legacy stock authority; not final FIFO allocation.");
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

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record LegacyAvailabilityRow(
        string? fs_kd_trs,
        string? fs_kd_barang,
        string? fs_kd_do,
        string? fs_kd_layanan,
        decimal fn_qty,
        string? fd_tgl_ed,
        string? fs_no_batch);
}

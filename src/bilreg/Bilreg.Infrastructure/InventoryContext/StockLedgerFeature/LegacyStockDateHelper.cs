using System.Globalization;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// Bidirectional mapping between ledger DateTime and legacy VARCHAR tgl/jam columns.
/// Shared by <see cref="LegacyStockReadPort"/> and <see cref="LegacyStockWriterPort"/>.
/// </summary>
internal static class LegacyStockDateHelper
{
    private static readonly string SentinelDate = StockLedgerSentinel.EmptyDate.ToString("yyyy-MM-dd");
    private static readonly string SentinelDateTime = StockLedgerSentinel.EmptyDate.ToString("yyyy-MM-dd HH:mm:ss");

    public static string FormatDate(DateTime value) =>
        value == StockLedgerSentinel.EmptyDate || value.Year >= 3000
            ? SentinelDate
            : value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string FormatTime(DateTime value) =>
        value == StockLedgerSentinel.EmptyDate || value.Year >= 3000
            ? "00:00:00"
            : value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime value) =>
        value == StockLedgerSentinel.EmptyDate || value.Year >= 3000
            ? SentinelDateTime
            : value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    public static string ToLegacyEdString(DateTime tglEd) => FormatDate(tglEd);

    public static DateTime ParseLegacyDate(string? value)
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

    public static DateTime ComposeMutasiDateTime(string? tglJam, string? tgl, string? jam)
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

    public static DateTime ComposeDateAndTime(string? tgl, string? jam)
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
}

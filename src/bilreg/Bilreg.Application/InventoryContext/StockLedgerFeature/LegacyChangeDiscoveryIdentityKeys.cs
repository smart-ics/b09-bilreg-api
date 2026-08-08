using System.Globalization;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S1 — Durable legacy identity encoding for SyncBatch / SourceConsequence idempotency keys.
/// Keys embed fingerprint-v1 material fields so set-diff can detect updates without a second hash implementation.
/// </summary>
public static class LegacyChangeDiscoveryIdentityKeys
{
    public const string JournalPrefix = "SYNC|BUKU|";
    public const string BalancePrefix = "SYNC|STOK|";

    public static string BuildJournalKey(
        IStockLedgerScopeKey scope,
        LegacyStockJournalEntryType journal)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(journal);
        return string.Join(
            '|',
            "SYNC",
            "BUKU",
            scope.BrgId.Trim(),
            scope.ReceiptSourceId.Trim(),
            journal.LayananId.Trim(),
            journal.LegacyJournalId.Trim(),
            journal.MutationKindId.Trim(),
            journal.MutationTransactionId.Trim(),
            FormatDecimal(journal.QuantityIn),
            FormatDecimal(journal.QuantityOut),
            FormatDecimal(journal.UnitCost),
            FormatDateOnly(journal.ExpirationDate),
            journal.Batch ?? string.Empty,
            FormatDateTime(journal.MutationTime),
            journal.PurchaseOrderId ?? string.Empty);
    }

    public static string BuildBalanceKey(
        IStockLedgerScopeKey scope,
        LegacyStockBalanceType balance)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(balance);
        if (string.IsNullOrWhiteSpace(balance.LegacyRowId))
            throw new ArgumentException("Legacy balance row id is required.", nameof(balance));

        return string.Join(
            '|',
            "SYNC",
            "STOK",
            scope.BrgId.Trim(),
            scope.ReceiptSourceId.Trim(),
            balance.LayananId.Trim(),
            balance.LegacyRowId.Trim(),
            FormatDecimal(balance.Quantity),
            FormatDecimal(balance.UnitCost),
            FormatDateOnly(balance.ExpirationDate),
            balance.Batch ?? string.Empty,
            balance.PurchaseOrderId ?? string.Empty);
    }

    public static bool TryParseJournalKey(
        string idempotencyKey,
        out LegacyJournalIdentity identity,
        out LegacyJournalMaterialSnapshot material)
    {
        identity = default;
        material = null!;
        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || !idempotencyKey.StartsWith(JournalPrefix, StringComparison.Ordinal))
            return false;

        var parts = idempotencyKey.Split('|');
        if (parts.Length != 15)
            return false;

        if (string.IsNullOrWhiteSpace(parts[4]) || string.IsNullOrWhiteSpace(parts[5]))
            return false;

        identity = new LegacyJournalIdentity(parts[4].Trim(), parts[5].Trim());
        material = new LegacyJournalMaterialSnapshot(
            MutationKindId: parts[6],
            MutationTransactionId: parts[7],
            QuantityIn: ParseDecimal(parts[8]),
            QuantityOut: ParseDecimal(parts[9]),
            UnitCost: ParseDecimal(parts[10]),
            ExpirationDate: ParseDateOnly(parts[11]),
            Batch: NullIfEmpty(parts[12]),
            MutationTime: ParseDateTime(parts[13]),
            PurchaseOrderId: NullIfEmpty(parts[14]));
        return true;
    }

    public static bool TryParseBalanceKey(
        string idempotencyKey,
        out LegacyBalanceIdentity identity,
        out LegacyBalanceMaterialSnapshot material)
    {
        identity = default;
        material = null!;
        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || !idempotencyKey.StartsWith(BalancePrefix, StringComparison.Ordinal))
            return false;

        var parts = idempotencyKey.Split('|');
        if (parts.Length != 11)
            return false;

        if (string.IsNullOrWhiteSpace(parts[4]) || string.IsNullOrWhiteSpace(parts[5]))
            return false;

        identity = new LegacyBalanceIdentity(parts[4].Trim(), parts[5].Trim());
        material = new LegacyBalanceMaterialSnapshot(
            Quantity: ParseDecimal(parts[6]),
            UnitCost: ParseDecimal(parts[7]),
            ExpirationDate: ParseDateOnly(parts[8]),
            Batch: NullIfEmpty(parts[9]),
            PurchaseOrderId: NullIfEmpty(parts[10]));
        return true;
    }

    private static string FormatDecimal(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string value)
        => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static string FormatDateTime(DateTime value)
        => value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTime ParseDateTime(string value)
        => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static string FormatDateOnly(DateOnly? value)
        => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static DateOnly? ParseDateOnly(string value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public readonly record struct LegacyJournalIdentity(string LayananId, string LegacyJournalId);

public readonly record struct LegacyBalanceIdentity(string LayananId, string LegacyRowId);

public sealed record LegacyJournalMaterialSnapshot(
    string MutationKindId,
    string MutationTransactionId,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal UnitCost,
    DateOnly? ExpirationDate,
    string? Batch,
    DateTime MutationTime,
    string? PurchaseOrderId);

public sealed record LegacyBalanceMaterialSnapshot(
    decimal Quantity,
    decimal UnitCost,
    DateOnly? ExpirationDate,
    string? Batch,
    string? PurchaseOrderId);

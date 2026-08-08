using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S2 — Computes a mechanism-neutral scoped legacy reconstruction basis fingerprint
/// from ordered P2-S1 snapshot DTOs (balances + journals).
/// <para>
/// Result is a freshness/basis token only: suitable for Phase C revalidation and for
/// initializing Synchronization Position after successful reconstruction. It does not
/// represent authority, ownership, or a watermark/cursor. Does not implement change
/// discovery, set-diff, catch-up, or Freshness Gate (Phase 3).
/// </para>
/// </summary>
public static class LegacyReconstructionBasisCalculator
{
    /// <summary>
    /// Explicit algorithm version stored with the opaque Synchronization Position.
    /// </summary>
    public const string AlgorithmVersion = "fingerprint-v1";

    private const string NullMarker = "\0";
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Hashes a scoped legacy snapshot into an opaque <see cref="SynchronizationPositionType"/>.
    /// Input lists are normalized to the same deterministic order as P2-S1 reads before hashing.
    /// </summary>
    public static SynchronizationPositionType Compute(
        IReadOnlyList<LegacyStockBalanceType> balances,
        IReadOnlyList<LegacyStockJournalEntryType> journals)
    {
        ArgumentNullException.ThrowIfNull(balances);
        ArgumentNullException.ThrowIfNull(journals);

        var orderedJournals = NormalizeJournals(journals);
        var orderedBalances = NormalizeBalances(balances);

        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Utf8, leaveOpen: true))
        {
            WriteString(writer, AlgorithmVersion);

            writer.Write('J');
            writer.Write(orderedJournals.Count);
            foreach (var row in orderedJournals)
                WriteJournal(writer, row);

            writer.Write('B');
            writer.Write(orderedBalances.Count);
            foreach (var row in orderedBalances)
                WriteBalance(writer, row);
        }

        var digest = SHA256.HashData(stream.ToArray());
        return SynchronizationPositionType.Create(digest, AlgorithmVersion);
    }

    private static List<LegacyStockJournalEntryType> NormalizeJournals(
        IReadOnlyList<LegacyStockJournalEntryType> journals)
        => journals
            .OrderBy(j => j.MutationTime)
            .ThenBy(j => j.LegacyJournalId, StringComparer.Ordinal)
            .ThenBy(j => j.LayananId, StringComparer.Ordinal)
            .ToList();

    private static List<LegacyStockBalanceType> NormalizeBalances(
        IReadOnlyList<LegacyStockBalanceType> balances)
        => balances
            .OrderBy(b => b.LayananId, StringComparer.Ordinal)
            .ThenBy(b => b.LegacyRowId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(b => b.BrgId, StringComparer.Ordinal)
            .ThenBy(b => b.ReceiptSourceId, StringComparer.Ordinal)
            .ThenBy(b => b.Quantity)
            .ThenBy(b => b.UnitCost)
            .ToList();

    private static void WriteJournal(BinaryWriter writer, LegacyStockJournalEntryType row)
    {
        WriteString(writer, row.LegacyJournalId);
        WriteString(writer, row.MutationKindId);
        WriteString(writer, row.MutationTransactionId);
        WriteString(writer, row.BrgId);
        WriteString(writer, row.ReceiptSourceId);
        WriteString(writer, row.LayananId);
        WriteDecimal(writer, row.QuantityIn);
        WriteDecimal(writer, row.QuantityOut);
        WriteDecimal(writer, row.UnitCost);
        WriteDateOnly(writer, row.ExpirationDate);
        WriteNullableString(writer, row.Batch);
        WriteDateTime(writer, row.MutationTime);
        WriteNullableString(writer, row.PurchaseOrderId);
    }

    private static void WriteBalance(BinaryWriter writer, LegacyStockBalanceType row)
    {
        WriteNullableString(writer, row.LegacyRowId);
        WriteString(writer, row.BrgId);
        WriteString(writer, row.ReceiptSourceId);
        WriteString(writer, row.LayananId);
        WriteDecimal(writer, row.Quantity);
        WriteDecimal(writer, row.UnitCost);
        WriteDateOnly(writer, row.ExpirationDate);
        WriteNullableString(writer, row.Batch);
        WriteNullableString(writer, row.PurchaseOrderId);
        // ReceiptTime / LastMutationTime intentionally excluded from fingerprint-v1
        // to avoid non-material column noise flipping the basis token.
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Utf8.GetBytes(value ?? string.Empty);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteNullableString(BinaryWriter writer, string? value)
    {
        if (value is null)
        {
            WriteString(writer, NullMarker);
            return;
        }

        WriteString(writer, value);
    }

    private static void WriteDecimal(BinaryWriter writer, decimal value)
        => WriteString(writer, value.ToString(CultureInfo.InvariantCulture));

    private static void WriteDateTime(BinaryWriter writer, DateTime value)
        => WriteString(writer, value.ToString("O", CultureInfo.InvariantCulture));

    private static void WriteDateOnly(BinaryWriter writer, DateOnly? value)
    {
        if (value is null)
        {
            WriteString(writer, NullMarker);
            return;
        }

        WriteString(writer, value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}

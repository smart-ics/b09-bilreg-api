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
/// P4-S1 / P4-S4 / P5-S2 / G-11 — Live DM receipt post + void, and MT transfer post
/// compatibility writer.
/// Receipt post: inserts authoritative <c>tb_buku</c> then <c>tb_stok</c>.
/// Void (Reversal): deletes/reduces <c>tb_stok</c> and inserts compensating <c>DO_V</c> journal
/// (<c>xVoidDelete=False</c> mode).
/// Transfer post: all <c>MT_OUT</c> deplete legs first, then all <c>MT_IN</c> insert legs,
/// from an allocation-explicit request (no independent FIFO/FEFO inside the writer).
/// Enlisted in the caller's ambient transaction. Does not transfer Stage B authority.
/// Not registered in production DI. Transfer void (<c>MT_*_V</c>) is P5-S4.
/// </summary>
public sealed class LegacyCompatibilityWriterPort : ILegacyCompatibilityWriterPort
{
    private const string MutationKindDo = "DO";
    private const string MutationKindDoVoid = "DO_V";
    private const string MutationKindMtOut = "MT_OUT";
    private const string MutationKindMtIn = "MT_IN";
    private const string PrefixBuku = "BK";
    private const string PrefixStok = "ST";
    private const string SentinelDate = "3000-01-01";
    private const string SentinelTime = "00:00:00";

    private readonly DatabaseOptions _opt;

    public LegacyCompatibilityWriterPort(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Apply(LegacyCompatibilityWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.JournalEntries.Count != request.BalanceMutations.Count)
        {
            throw new InvalidOperationException(
                "Legacy compatibility write requires JournalEntries and BalanceMutations to be paired 1:1 " +
                $"(journals={request.JournalEntries.Count}, balances={request.BalanceMutations.Count}).");
        }

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();

        if (request.MovementKind == StockMovementKindEnum.Receipt)
        {
            for (var i = 0; i < request.BalanceMutations.Count; i++)
            {
                var balance = request.BalanceMutations[i];
                var journal = request.JournalEntries[i];
                ValidateReceiptLine(balance, journal, i);
                InsertBukuThenStok(conn, balance, journal, MutationKindDo);
            }

            return;
        }

        if (request.MovementKind == StockMovementKindEnum.Reversal)
        {
            for (var i = 0; i < request.BalanceMutations.Count; i++)
            {
                var balance = request.BalanceMutations[i];
                var journal = request.JournalEntries[i];
                ValidateVoidLine(balance, journal, i);
                ApplyVoidLine(conn, balance, journal);
            }

            return;
        }

        if (request.MovementKind == StockMovementKindEnum.Transfer)
        {
            ValidateTransferPostRequest(request);

            foreach (var (balance, journal) in EnumerateTransferLines(request, MutationKindMtOut))
                ApplyTransferOutLine(conn, balance, journal);

            foreach (var (balance, journal) in EnumerateTransferLines(request, MutationKindMtIn))
                ApplyTransferInLine(conn, balance, journal);

            return;
        }

        throw new NotSupportedException(
            $"LegacyCompatibilityWriterPort supports Receipt post, Reversal void, and Transfer post only; " +
            $"movement kind '{request.MovementKind}' is not supported.");
    }

    private static void ValidateReceiptLine(
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal,
        int index)
    {
        if (balance.Action != LegacyBalanceMutationActionEnum.Upsert)
        {
            throw new NotSupportedException(
                $"DM receipt post supports Upsert balance action only (line {index}); " +
                $"got '{balance.Action}'.");
        }

        if (journal.IsVoid)
        {
            throw new NotSupportedException(
                $"DM receipt post does not support void journal entries (line {index}).");
        }

        ValidateSharedIdentity(balance, journal, index);

        var mutationKind = string.IsNullOrWhiteSpace(journal.MutationKindId)
            ? MutationKindDo
            : journal.MutationKindId.Trim();
        if (!string.Equals(mutationKind, MutationKindDo, StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"DM receipt post requires MutationKindId '{MutationKindDo}' (line {index}); " +
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

    private static void ValidateVoidLine(
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal,
        int index)
    {
        if (balance.Action is not (
                LegacyBalanceMutationActionEnum.Delete
                or LegacyBalanceMutationActionEnum.Upsert))
        {
            throw new NotSupportedException(
                $"DM receipt void supports Delete or Upsert balance action only (line {index}); " +
                $"got '{balance.Action}'.");
        }

        if (!journal.IsVoid)
        {
            throw new InvalidOperationException(
                $"DM receipt void journal line {index} requires IsVoid = true.");
        }

        ValidateSharedIdentity(balance, journal, index);

        if (string.IsNullOrWhiteSpace(balance.LegacyRowId))
        {
            throw new InvalidOperationException(
                $"DM receipt void balance line {index} requires LegacyRowId (targeted ST* row).");
        }

        var mutationKind = string.IsNullOrWhiteSpace(journal.MutationKindId)
            ? string.Empty
            : journal.MutationKindId.Trim();
        if (!string.Equals(mutationKind, MutationKindDoVoid, StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                $"DM receipt void requires MutationKindId '{MutationKindDoVoid}' (line {index}); " +
                $"got '{mutationKind}'.");
        }

        if (journal.QuantityIn != 0m)
        {
            throw new InvalidOperationException(
                $"DM receipt void journal line {index} must have QuantityIn = 0 (got {journal.QuantityIn}).");
        }

        if (journal.QuantityOut <= 0m || balance.Quantity <= 0m)
        {
            throw new InvalidOperationException(
                $"DM receipt void line {index} requires positive outbound quantity.");
        }

        if (journal.QuantityOut != balance.Quantity)
        {
            throw new InvalidOperationException(
                $"DM receipt void line {index} journal QuantityOut ({journal.QuantityOut}) " +
                $"must equal balance Quantity ({balance.Quantity}).");
        }
    }

    private static void ValidateTransferPostRequest(LegacyCompatibilityWriteRequest request)
    {
        if (request.BalanceMutations.Count == 0)
        {
            throw new InvalidOperationException(
                "MT transfer post requires at least one OUT/IN line pair.");
        }

        var outLines = new List<(int Index, LegacyCompatibilityBalanceMutationType Balance, LegacyCompatibilityJournalEntryType Journal)>();
        var inLines = new List<(int Index, LegacyCompatibilityBalanceMutationType Balance, LegacyCompatibilityJournalEntryType Journal)>();

        for (var i = 0; i < request.BalanceMutations.Count; i++)
        {
            var balance = request.BalanceMutations[i];
            var journal = request.JournalEntries[i];

            if (journal.IsVoid)
            {
                throw new NotSupportedException(
                    $"MT transfer post does not support void journal entries (line {i}); " +
                    "transfer void (MT_IN_V / MT_OUT_V) is P5-S4.");
            }

            ValidateSharedIdentity(balance, journal, i);

            var mutationKind = string.IsNullOrWhiteSpace(journal.MutationKindId)
                ? string.Empty
                : journal.MutationKindId.Trim();

            if (string.Equals(mutationKind, MutationKindMtOut, StringComparison.Ordinal))
            {
                ValidateTransferOutLine(balance, journal, i);
                outLines.Add((i, balance, journal));
            }
            else if (string.Equals(mutationKind, MutationKindMtIn, StringComparison.Ordinal))
            {
                ValidateTransferInLine(balance, journal, i);
                inLines.Add((i, balance, journal));
            }
            else
            {
                throw new NotSupportedException(
                    $"MT transfer post requires MutationKindId '{MutationKindMtOut}' or '{MutationKindMtIn}' " +
                    $"(line {i}); got '{mutationKind}'.");
            }
        }

        if (outLines.Count == 0 || inLines.Count == 0)
        {
            throw new InvalidOperationException(
                $"MT transfer post requires both OUT and IN legs " +
                $"(out={outLines.Count}, in={inLines.Count}).");
        }

        if (outLines.Count != inLines.Count)
        {
            throw new InvalidOperationException(
                $"MT transfer post requires equal OUT and IN line counts " +
                $"(out={outLines.Count}, in={inLines.Count}).");
        }

        for (var pair = 0; pair < outLines.Count; pair++)
        {
            var (_, outBalance, outJournal) = outLines[pair];
            var (_, inBalance, inJournal) = inLines[pair];
            ValidateTransferPairConservation(outBalance, outJournal, inBalance, inJournal, pair);
        }
    }

    private static void ValidateTransferOutLine(
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal,
        int index)
    {
        if (balance.Action is not (
                LegacyBalanceMutationActionEnum.Delete
                or LegacyBalanceMutationActionEnum.Upsert))
        {
            throw new NotSupportedException(
                $"MT_OUT supports Delete or Upsert balance action only (line {index}); " +
                $"got '{balance.Action}'.");
        }

        if (string.IsNullOrWhiteSpace(balance.LegacyRowId))
        {
            throw new InvalidOperationException(
                $"MT_OUT balance line {index} requires LegacyRowId (targeted source ST* row).");
        }

        if (string.IsNullOrWhiteSpace(journal.MutationTransactionId))
        {
            throw new InvalidOperationException(
                $"MT_OUT journal line {index} requires MutationTransactionId (MT transaction id).");
        }

        if (journal.QuantityIn != 0m)
        {
            throw new InvalidOperationException(
                $"MT_OUT journal line {index} must have QuantityIn = 0 (got {journal.QuantityIn}).");
        }

        if (journal.QuantityOut <= 0m || balance.Quantity <= 0m)
        {
            throw new InvalidOperationException(
                $"MT_OUT line {index} requires positive outbound quantity.");
        }

        if (journal.QuantityOut != balance.Quantity)
        {
            throw new InvalidOperationException(
                $"MT_OUT line {index} journal QuantityOut ({journal.QuantityOut}) " +
                $"must equal balance Quantity ({balance.Quantity}).");
        }
    }

    private static void ValidateTransferInLine(
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal,
        int index)
    {
        if (balance.Action != LegacyBalanceMutationActionEnum.Upsert)
        {
            throw new NotSupportedException(
                $"MT_IN supports Upsert balance action only (line {index}); " +
                $"got '{balance.Action}'.");
        }

        if (string.IsNullOrWhiteSpace(journal.MutationTransactionId))
        {
            throw new InvalidOperationException(
                $"MT_IN journal line {index} requires MutationTransactionId (MT transaction id).");
        }

        if (journal.QuantityOut != 0m)
        {
            throw new InvalidOperationException(
                $"MT_IN journal line {index} must have QuantityOut = 0 (got {journal.QuantityOut}).");
        }

        if (journal.QuantityIn <= 0m || balance.Quantity <= 0m)
        {
            throw new InvalidOperationException(
                $"MT_IN line {index} requires positive inbound quantity.");
        }

        if (journal.QuantityIn != balance.Quantity)
        {
            throw new InvalidOperationException(
                $"MT_IN line {index} journal QuantityIn ({journal.QuantityIn}) " +
                $"must equal balance Quantity ({balance.Quantity}).");
        }
    }

    private static void ValidateTransferPairConservation(
        LegacyCompatibilityBalanceMutationType outBalance,
        LegacyCompatibilityJournalEntryType outJournal,
        LegacyCompatibilityBalanceMutationType inBalance,
        LegacyCompatibilityJournalEntryType inJournal,
        int pairIndex)
    {
        if (!string.Equals(outBalance.BrgId.Trim(), inBalance.BrgId.Trim(), StringComparison.Ordinal) ||
            !string.Equals(outBalance.ReceiptSourceId.Trim(), inBalance.ReceiptSourceId.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} OUT/IN BrgId or ReceiptSourceId mismatch.");
        }

        if (outBalance.Quantity != inBalance.Quantity)
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} quantity not conserved " +
                $"(out={outBalance.Quantity}, in={inBalance.Quantity}).");
        }

        if (outBalance.UnitCost != inBalance.UnitCost)
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} UnitCost (HPP) not conserved " +
                $"(out={outBalance.UnitCost}, in={inBalance.UnitCost}).");
        }

        var outEd = outBalance.ExpirationDate ?? outJournal.ExpirationDate;
        var inEd = inBalance.ExpirationDate ?? inJournal.ExpirationDate;
        if (outEd != inEd)
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} ExpirationDate not conserved.");
        }

        var outBatch = NullToEmpty(outBalance.Batch ?? outJournal.Batch);
        var inBatch = NullToEmpty(inBalance.Batch ?? inJournal.Batch);
        if (!string.Equals(outBatch, inBatch, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} Batch not conserved.");
        }

        var outPo = NullToEmpty(outBalance.PurchaseOrderId);
        var inPo = NullToEmpty(inBalance.PurchaseOrderId);
        if (!string.Equals(outPo, inPo, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} PurchaseOrderId not conserved.");
        }

        if (!string.Equals(
                outJournal.MutationTransactionId.Trim(),
                inJournal.MutationTransactionId.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} MutationTransactionId (MT id) not conserved.");
        }

        if (string.Equals(
                outBalance.LayananId.Trim(),
                inBalance.LayananId.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"MT transfer pair {pairIndex} source and destination LayananId must differ.");
        }
    }

    private static void ValidateSharedIdentity(
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal,
        int index)
    {
        if (string.IsNullOrWhiteSpace(balance.BrgId) || string.IsNullOrWhiteSpace(balance.ReceiptSourceId))
            throw new InvalidOperationException($"DM balance line {index} requires BrgId and ReceiptSourceId.");

        if (string.IsNullOrWhiteSpace(journal.BrgId) || string.IsNullOrWhiteSpace(journal.ReceiptSourceId))
            throw new InvalidOperationException($"DM journal line {index} requires BrgId and ReceiptSourceId.");

        if (!string.Equals(balance.BrgId.Trim(), journal.BrgId.Trim(), StringComparison.Ordinal) ||
            !string.Equals(balance.ReceiptSourceId.Trim(), journal.ReceiptSourceId.Trim(), StringComparison.Ordinal) ||
            !string.Equals(balance.LayananId.Trim(), journal.LayananId.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"DM line {index} balance/journal identity mismatch (BrgId/ReceiptSourceId/LayananId).");
        }
    }

    private static IEnumerable<(LegacyCompatibilityBalanceMutationType Balance, LegacyCompatibilityJournalEntryType Journal)>
        EnumerateTransferLines(LegacyCompatibilityWriteRequest request, string mutationKind)
    {
        for (var i = 0; i < request.BalanceMutations.Count; i++)
        {
            var journal = request.JournalEntries[i];
            var kind = string.IsNullOrWhiteSpace(journal.MutationKindId)
                ? string.Empty
                : journal.MutationKindId.Trim();
            if (string.Equals(kind, mutationKind, StringComparison.Ordinal))
                yield return (request.BalanceMutations[i], journal);
        }
    }

    private static void InsertBukuThenStok(
        SqlConnection conn,
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal,
        string mutationKind)
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
        // P4-S2: honor pre-assigned ids when present so fingerprint can be computed before Apply.
        var bukuId = string.IsNullOrWhiteSpace(journal.LegacyJournalId)
            ? NunaId.NewLegacyCompact(PrefixBuku)
            : journal.LegacyJournalId.Trim();
        InsertBuku(conn, new BukuInsert(
            bukuId, brgId, layananId, po, doId, expiration, batch,
            qty, 0m, hpp, mutasiId, mutasiDate, mutasiTime, mutasiCombined, mutationKind, satuan));

        var stokId = string.IsNullOrWhiteSpace(balance.LegacyRowId)
            ? NunaId.NewLegacyCompact(PrefixStok)
            : balance.LegacyRowId.Trim();
        InsertStok(conn, new StokInsert(
            stokId, brgId, layananId, po, doId, expiration, batch,
            qty, qty, hpp, mutasiId, mutasiDate, mutasiTime, satuan));
    }

    /// <summary>
    /// VB6 RemoveStok (xVoidDelete=False): reduce/delete targeted tb_stok, then INSERT DO_V journal.
    /// Application supplies the ST* target; no silent FIFO re-selection.
    /// </summary>
    private static void ApplyVoidLine(
        SqlConnection conn,
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal)
    {
        var stokId = balance.LegacyRowId!.Trim();
        var qtyOut = balance.Quantity;

        DepleteStokRow(conn, stokId, qtyOut, balance.Action, contextLabel: "DM receipt void");

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
        var hpp = balance.UnitCost;

        var bukuId = string.IsNullOrWhiteSpace(journal.LegacyJournalId)
            ? NunaId.NewLegacyCompact(PrefixBuku)
            : journal.LegacyJournalId.Trim();

        InsertBuku(conn, new BukuInsert(
            bukuId, brgId, layananId, po, doId, expiration, batch,
            0m, qtyOut, hpp, mutasiId, mutasiDate, mutasiTime, mutasiCombined, MutationKindDoVoid, satuan));
    }

    /// <summary>
    /// MT_OUT: deplete targeted source tb_stok (allocation-explicit LegacyRowId), then INSERT MT_OUT journal.
    /// No independent FIFO/FEFO selection.
    /// </summary>
    private static void ApplyTransferOutLine(
        SqlConnection conn,
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal)
    {
        var stokId = balance.LegacyRowId!.Trim();
        var qtyOut = balance.Quantity;

        DepleteStokRow(conn, stokId, qtyOut, balance.Action, contextLabel: "MT_OUT");

        var mutationTime = journal.MutationTime;
        var mutasiDate = FormatDate(mutationTime);
        var mutasiTime = FormatTime(mutationTime);
        var mutasiCombined = FormatDateTime(mutationTime);
        var expiration = FormatExpiration(balance.ExpirationDate ?? journal.ExpirationDate);
        var batch = NullToEmpty(balance.Batch ?? journal.Batch);
        var po = NullToEmpty(balance.PurchaseOrderId);
        var satuan = NullToEmpty(balance.SmallestUnitId ?? journal.SmallestUnitId);
        var mutasiId = journal.MutationTransactionId.Trim();
        var doId = balance.ReceiptSourceId.Trim();
        var brgId = balance.BrgId.Trim();
        var layananId = balance.LayananId.Trim();
        var hpp = balance.UnitCost;

        var bukuId = string.IsNullOrWhiteSpace(journal.LegacyJournalId)
            ? NunaId.NewLegacyCompact(PrefixBuku)
            : journal.LegacyJournalId.Trim();

        InsertBuku(conn, new BukuInsert(
            bukuId, brgId, layananId, po, doId, expiration, batch,
            0m, qtyOut, hpp, mutasiId, mutasiDate, mutasiTime, mutasiCombined, MutationKindMtOut, satuan));
    }

    /// <summary>
    /// MT_IN: insert destination tb_buku then tb_stok from explicit IN-line provenance
    /// (caller mirrors OUT facts; writer does not re-read OUT journals).
    /// </summary>
    private static void ApplyTransferInLine(
        SqlConnection conn,
        LegacyCompatibilityBalanceMutationType balance,
        LegacyCompatibilityJournalEntryType journal)
    {
        InsertBukuThenStok(conn, balance, journal, MutationKindMtIn);
    }

    /// <summary>
    /// Shared UPDLOCK deplete for DM void and MT_OUT. Full depletion deletes the row
    /// (legacy zero-row deletion); partial reduces <c>fn_qty</c>.
    /// </summary>
    private static void DepleteStokRow(
        SqlConnection conn,
        string stokId,
        decimal qtyOut,
        LegacyBalanceMutationActionEnum action,
        string contextLabel)
    {
        var currentQty = conn.ExecuteScalar<decimal?>(
            """
            SELECT fn_qty FROM tb_stok WITH (UPDLOCK, ROWLOCK)
            WHERE fs_kd_trs = @fs_kd_trs
            """,
            new { fs_kd_trs = stokId });

        if (currentQty is null)
        {
            throw new InvalidOperationException(
                $"{contextLabel} cannot find tb_stok row '{stokId}'.");
        }

        if (currentQty.Value < qtyOut)
        {
            throw new InvalidOperationException(
                $"{contextLabel} insufficient tb_stok qty on '{stokId}' " +
                $"(available {currentQty.Value}, required {qtyOut}).");
        }

        if (action == LegacyBalanceMutationActionEnum.Delete
            || currentQty.Value == qtyOut)
        {
            var deleted = conn.Execute(
                """
                DELETE FROM tb_stok
                WHERE fs_kd_trs = @fs_kd_trs AND fn_qty = @fn_qty
                """,
                new { fs_kd_trs = stokId, fn_qty = currentQty.Value });
            if (deleted != 1)
            {
                throw new InvalidOperationException(
                    $"{contextLabel} failed to DELETE tb_stok '{stokId}' (rows={deleted}).");
            }
        }
        else
        {
            var remaining = currentQty.Value - qtyOut;
            var updated = conn.Execute(
                """
                UPDATE tb_stok
                SET fn_qty = @fn_qty
                WHERE fs_kd_trs = @fs_kd_trs AND fn_qty = @expected_qty
                """,
                new
                {
                    fs_kd_trs = stokId,
                    fn_qty = remaining,
                    expected_qty = currentQty.Value
                });
            if (updated != 1)
            {
                throw new InvalidOperationException(
                    $"{contextLabel} failed to UPDATE tb_stok '{stokId}' (rows={updated}).");
            }
        }
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

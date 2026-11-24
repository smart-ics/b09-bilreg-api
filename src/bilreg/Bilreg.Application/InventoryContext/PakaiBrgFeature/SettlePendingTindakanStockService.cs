using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using MediatR;
using Microsoft.Extensions.Logging;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature;

/// <summary>
/// TD-04 — Pelunasan antrean <c>BILRG_TindakanStokPending</c> secara FIFO.
/// <paramref name="BrgId"/> / <paramref name="LayananId"/> kosong = seluruh scope yang punya antrean aktif.
/// </summary>
public record SettlePendingTindakanStockCommand(
    string BrgId,
    string LayananId,
    string UserId) : IRequest<SettlePendingTindakanStockResult>;

public record SettledPendingItem(
    string PendingId,
    string RegId,
    string BrgId,
    string LayananId,
    string PakaiBrgId);

public record SettlePendingTindakanStockResult(
    int SettledCount,
    IReadOnlyList<string> PakaiBrgIds,
    IReadOnlyList<SettledPendingItem> Items);

public interface ISettlePendingTindakanStockService
{
    Task<SettlePendingTindakanStockResult> Settle(
        SettlePendingTindakanStockCommand command,
        CancellationToken cancellationToken = default);
}

public class SettlePendingTindakanStockHandler :
    ISettlePendingTindakanStockService,
    IRequestHandler<SettlePendingTindakanStockCommand, SettlePendingTindakanStockResult>
{
    private const int MAX_KETERANGAN_LENGTH = 40;

    private readonly ITindakanStokPendingRepo _pendingRepo;
    private readonly IPakaiBrgInternalService _pakaiBrgInternalService;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly ILogger<SettlePendingTindakanStockHandler>? _logger;

    public SettlePendingTindakanStockHandler(
        ITindakanStokPendingRepo pendingRepo,
        IPakaiBrgInternalService pakaiBrgInternalService,
        ITglJamProvider tglJamProvider,
        ILegacyStockReadPort legacyStockReadPort,
        ILogger<SettlePendingTindakanStockHandler>? logger = null)
    {
        _pendingRepo = pendingRepo;
        _pakaiBrgInternalService = pakaiBrgInternalService;
        _tglJamProvider = tglJamProvider;
        _legacyStockReadPort = legacyStockReadPort;
        _logger = logger;
    }

    public Task<SettlePendingTindakanStockResult> Handle(
        SettlePendingTindakanStockCommand request,
        CancellationToken cancellationToken) => Settle(request, cancellationToken);

    public async Task<SettlePendingTindakanStockResult> Settle(
        SettlePendingTindakanStockCommand command,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(command, nameof(command));
        Guard.Against.NullOrWhiteSpace(command.UserId, nameof(command.UserId));

        var brgId = (command.BrgId ?? string.Empty).Trim();
        var layananId = (command.LayananId ?? string.Empty).Trim();

        var pendingList = _pendingRepo.ListFifoAktif(brgId, layananId)
            .Where(x => !x.StatusPotStok && !x.IsVoid && x.QtyPending > 0m)
            .ToList();

        var pakaiBrgIds = new List<string>();
        var items = new List<SettledPendingItem>();

        var scopes = pendingList
            .GroupBy(x => (BrgId: x.BrgId.Trim(), LayananId: x.LayananId.Trim()))
            .Select(g => (g.Key.BrgId, g.Key.LayananId, Rows: g.ToList()));

        foreach (var scope in scopes)
        {
            await SettleScope(scope.BrgId, scope.LayananId, scope.Rows, command.UserId, pakaiBrgIds, items, cancellationToken);
        }

        return new SettlePendingTindakanStockResult(items.Count, pakaiBrgIds, items);
    }

    private async Task SettleScope(
        string brgId,
        string layananId,
        List<TindakanStokPendingType> rows,
        string userId,
        List<string> pakaiBrgIds,
        List<SettledPendingItem> items,
        CancellationToken ct)
    {
        decimal availableWhole;
        try
        {
            availableWhole = Math.Floor(
                _legacyStockReadPort.ListBalancesAtLocation(brgId, layananId)
                    .Where(c => c.QtySisa > 0m)
                    .Sum(c => c.QtySisa));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex,
                "Settle pending dilewati untuk Brg {BrgId} Layanan {LayananId}: gagal membaca saldo legacy.",
                brgId, layananId);
            return;
        }

        // Fase 1: baris utuh (QtyPending >= 1), FIFO ketat — berhenti saat saldo tidak cukup.
        foreach (var row in rows.Where(r => r.QtyPending >= 1m))
        {
            var qtyCut = Math.Floor(row.QtyPending);
            if (availableWhole < qtyCut)
                break;

            var pakai = await IssuePakai(row, qtyCut, userId, ct);
            _pendingRepo.MarkSettled(row.PendingId, pakai.PakaiBrgId, qtyCut, _tglJamProvider.Now, userId);
            availableWhole -= qtyCut;

            pakaiBrgIds.Add(pakai.PakaiBrgId);
            items.Add(new SettledPendingItem(row.PendingId, row.RegId, brgId, layananId, pakai.PakaiBrgId));
        }

        // Fase 2: akumulasi pecahan lintas baris (0 < QtyPending < 1).
        var fractions = new Queue<TindakanStokPendingType>(rows.Where(r => r.QtyPending is > 0m and < 1m));
        while (availableWhole >= 1m && fractions.Count > 0)
        {
            var batch = new List<TindakanStokPendingType>();
            decimal sum = 0m;
            while (sum < 1m && fractions.Count > 0)
            {
                var next = fractions.Dequeue();
                batch.Add(next);
                sum += next.QtyPending;
            }

            if (sum < 1m)
                break;

            var first = batch[0];
            var pakai = await IssuePakai(first, 1m, userId, ct);
            availableWhole -= 1m;
            pakaiBrgIds.Add(pakai.PakaiBrgId);

            foreach (var row in batch)
            {
                _pendingRepo.MarkSettled(row.PendingId, pakai.PakaiBrgId, row.QtyPending, _tglJamProvider.Now, userId);
                items.Add(new SettledPendingItem(row.PendingId, row.RegId, brgId, layananId, pakai.PakaiBrgId));
            }
        }
    }

    private Task<PakaiBrgCreateFromTindakanResult> IssuePakai(
        TindakanStokPendingType row,
        decimal qty,
        string userId,
        CancellationToken ct)
    {
        var keterangan = PakaiBrgInternalService.TruncateSafe($"Pot-Tdk {row.TindakanId}", MAX_KETERANGAN_LENGTH);
        var cmd = new PakaiBrgCreateFromTindakanCommand(
            row.RegId,
            row.LayananId,
            userId,
            keterangan,
            [new PakaiBrgCreateFromTindakanItem(row.BrgId, qty, KeteranganItem: keterangan)]);
        return _pakaiBrgInternalService.CreateFromTindakan(cmd, ct);
    }
}

/// <summary>
/// Pemicu auto-drain pasca-commit mutasi stok masuk. Tidak pernah melempar ke alur mutasi pemicu.
/// </summary>
public static class PendingSettleTrigger
{
    public static async Task TryDrain(
        ISender? sender,
        ILogger? logger,
        string userId,
        IEnumerable<(string BrgId, string LayananId)> scopes,
        CancellationToken ct = default)
    {
        if (sender is null)
            return;

        foreach (var scope in scopes
                     .Where(s => !string.IsNullOrWhiteSpace(s.BrgId))
                     .Distinct())
        {
            try
            {
                await sender.Send(new SettlePendingTindakanStockCommand(scope.BrgId, scope.LayananId, userId), ct);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex,
                    "Auto-drain pending stok tindakan gagal untuk Brg {BrgId} Layanan {LayananId}; dicoba ulang pada siklus berikutnya.",
                    scope.BrgId, scope.LayananId);
            }
        }
    }
}

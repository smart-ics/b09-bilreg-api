using Ardalis.GuardClauses;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.IgdContext;

public class IgdStockService : IIgdStockService
{
    private readonly IMediator _mediator;
    private readonly ITarifRepo _tarifRepo;
    private readonly ITarifBarangRepo _tarifBarangRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ILegacyStockReadPort _legacyStockReadPort;
    private readonly ITindakanStokPendingRepo _tindakanStokPendingRepo;

    public IgdStockService(
        IMediator mediator,
        ITarifRepo tarifRepo,
        ITarifBarangRepo tarifBarangRepo,
        IBrgRepo brgRepo,
        ILegacyStockReadPort legacyStockReadPort,
        ITindakanStokPendingRepo tindakanStokPendingRepo)
    {
        _mediator = mediator;
        _tarifRepo = tarifRepo;
        _tarifBarangRepo = tarifBarangRepo;
        _brgRepo = brgRepo;
        _legacyStockReadPort = legacyStockReadPort;
        _tindakanStokPendingRepo = tindakanStokPendingRepo;
    }

    public async Task<string> DeductBhpAsync(
        string layananId,
        string brgId,
        decimal qty,
        string userId,
        string keterangan,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NegativeOrZero(qty, nameof(qty));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var cmd = new PakaiBrgCreateCommand(
            LayananId: layananId.Trim(),
            UserId: userId.Trim(),
            Keterangan: keterangan ?? string.Empty,
            ListItem: [new PakaiBrgCreateItemRequest(brgId.Trim(), qty)]);

        var result = await _mediator.Send(cmd, cancellationToken);
        return result.PakaiBrgId;
    }

    public async Task<IgdBomProcessResult> ProcessTindakanBomAsync(
        string tindakanIgdId,
        string tarifId,
        int tindakanQty,
        string layananId,
        string regId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(tindakanIgdId, nameof(tindakanIgdId));
        Guard.Against.NullOrWhiteSpace(layananId, nameof(layananId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var trimmedTarifId = (tarifId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmedTarifId))
        {
            return new IgdBomProcessResult(string.Empty, false, []);
        }

        var trimmedLayananId = layananId.Trim();
        var trimmedRegId = (regId ?? string.Empty).Trim();
        var effectiveQty = tindakanQty <= 0 ? 1 : tindakanQty;

        // 1. Load Tarif (IsChargeBarang) and BOM items
        var isChargeBarang = _tarifRepo.LoadEntity(TarifType.Key(trimmedTarifId))
            .Match(t => t.IsChargeBarang, () => false);

        var bomList = _tarifBarangRepo.ListData(TarifType.Key(trimmedTarifId))?
            .OrderBy(x => x.NoUrut)
            .ToList() ?? [];

        if (bomList.Count == 0)
        {
            return new IgdBomProcessResult(string.Empty, isChargeBarang, []);
        }

        // 2. Resolve goods & convert BOM UoM to SatuanKecil
        var resolvedItems = new List<ResolvedBomItem>();
        foreach (var bom in bomList)
        {
            var brg = _brgRepo.LoadEntity(new BrgKey(bom.BrgId))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"Barang dengan Id '{bom.BrgId}' tidak ditemukan."));

            var satuanBomId = (bom.SatuanId ?? string.Empty).Trim();
            var satuanKecilId = (brg.SatuanKecil?.SatuanId ?? string.Empty).Trim();
            var konversi = ResolveKonversi(bom, brg, satuanBomId, satuanKecilId);

            var qtyBomTotal = bom.QtyBarang * effectiveQty;
            var qtyKecilRequested = qtyBomTotal * konversi;

            resolvedItems.Add(new ResolvedBomItem(
                bom,
                satuanKecilId,
                qtyBomTotal,
                qtyKecilRequested));
        }

        // 3. Check available stock per BrgId at IGD Layanan (failures route to pending stock)
        var availableStockByBrg = LoadAvailableStockByBrg(resolvedItems, trimmedLayananId);

        var calculatedItems = new List<CalculatedItem>();
        foreach (var item in resolvedItems)
        {
            var currentAvailable = availableStockByBrg[item.Bom.BrgId];
            var availableWhole = Math.Floor(currentAvailable);
            var requestedWhole = Math.Floor(item.QtyKecilRequested);
            var qtyImmediateCut = Math.Min(requestedWhole, availableWhole);
            var qtyPending = item.QtyKecilRequested - qtyImmediateCut;

            availableStockByBrg[item.Bom.BrgId] = Math.Max(0m, currentAvailable - qtyImmediateCut);

            calculatedItems.Add(new CalculatedItem(item, qtyImmediateCut, qtyPending));
        }

        // 4. Deduct immediately available quantities via PakaiBrgCreateCommand (failures route to pending stock)
        var immediateCutItems = calculatedItems
            .Where(x => x.QtyImmediateCut >= 1m)
            .Select(x => new PakaiBrgCreateItemRequest(x.Resolved.Bom.BrgId, x.QtyImmediateCut))
            .ToList();

        var pakaiBrgId = string.Empty;
        if (immediateCutItems.Count > 0)
        {
            var pakaiCmd = new PakaiBrgCreateCommand(
                LayananId: trimmedLayananId,
                UserId: userId.Trim(),
                Keterangan: $"TDK-IGD:{tindakanIgdId} {trimmedTarifId}",
                ListItem: immediateCutItems);

            try
            {
                var pakaiResult = await _mediator.Send(pakaiCmd, cancellationToken);
                pakaiBrgId = pakaiResult.PakaiBrgId;
            }
            catch
            {
                // Stock deduction failure must not block clinical IGD procedure saving;
                // route all requested quantities to BILRG_TindakanStokPending.
                pakaiBrgId = string.Empty;
                calculatedItems = calculatedItems
                    .Select(x => new CalculatedItem(x.Resolved, 0m, x.Resolved.QtyKecilRequested))
                    .ToList();
            }
        }

        // 5. Build TindakanIgdBarangType snapshot & persist pending stock records
        var listBarang = new List<TindakanIgdBarangType>();
        var listPending = new List<TindakanStokPendingType>();

        foreach (var calc in calculatedItems)
        {
            var itemPakaiBrgId = calc.QtyImmediateCut >= 1m ? pakaiBrgId : string.Empty;
            var trsBillingId = isChargeBarang ? $"{tindakanIgdId}-B{calc.Resolved.Bom.NoUrut:D2}" : string.Empty;

            listBarang.Add(new TindakanIgdBarangType(
                NoUrut: calc.Resolved.Bom.NoUrut,
                BrgId: calc.Resolved.Bom.BrgId,
                SatuanBomId: calc.Resolved.Bom.SatuanId,
                QtyBom: calc.Resolved.QtyBomTotal,
                SatuanKecilId: calc.Resolved.SatuanKecilId,
                QtyKecilRequested: calc.Resolved.QtyKecilRequested,
                QtyKecilDeducted: calc.QtyImmediateCut,
                QtyKecilPending: calc.QtyPending,
                IsChargeBarang: isChargeBarang,
                PakaiBrgId: itemPakaiBrgId,
                TrsBillingId: trsBillingId,
                IsVoid: false));

            if (calc.QtyPending > 0m)
            {
                listPending.Add(new TindakanStokPendingType(
                    PendingId: NunaId.New("PND"),
                    TindakanId: tindakanIgdId,
                    TindakanBarangId: string.Empty,
                    NoUrut: calc.Resolved.Bom.NoUrut,
                    RegId: trimmedRegId,
                    LayananId: trimmedLayananId,
                    BrgId: calc.Resolved.Bom.BrgId,
                    SatuanKecilId: calc.Resolved.SatuanKecilId,
                    QtyPending: calc.QtyPending,
                    QtySettled: 0m,
                    StatusPotStok: false,
                    PakaiBrgId: string.Empty,
                    IsVoid: false));
            }
        }

        if (listPending.Count > 0)
        {
            _tindakanStokPendingRepo.Insert(listPending, userId.Trim());
        }

        return new IgdBomProcessResult(pakaiBrgId, isChargeBarang, listBarang);
    }

    public async Task VoidTindakanStockAsync(
        string tindakanIgdId,
        string headerPakaiBrgId,
        IEnumerable<string>? itemPakaiBrgIds,
        string userId,
        string voidReason,
        DateTime voidDate,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(tindakanIgdId, nameof(tindakanIgdId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(voidReason, nameof(voidReason));

        var pakaiBrgIdsToVoid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(headerPakaiBrgId))
            pakaiBrgIdsToVoid.Add(headerPakaiBrgId.Trim());

        if (itemPakaiBrgIds is not null)
        {
            foreach (var id in itemPakaiBrgIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    pakaiBrgIdsToVoid.Add(id.Trim());
            }
        }

        var pendingRows = _tindakanStokPendingRepo.ListByTindakanId(tindakanIgdId) ?? [];
        foreach (var pending in pendingRows)
        {
            if (!pending.IsVoid && !string.IsNullOrWhiteSpace(pending.PakaiBrgId))
                pakaiBrgIdsToVoid.Add(pending.PakaiBrgId.Trim());
        }

        foreach (var pakaiBrgId in pakaiBrgIdsToVoid)
        {
            await _mediator.Send(
                new PakaiBrgVoidCommand(pakaiBrgId, userId.Trim(), voidReason.Trim()),
                cancellationToken);
        }

        _tindakanStokPendingRepo.MarkVoidByTindakanId(tindakanIgdId, voidDate, userId.Trim());
    }

    private static decimal ResolveKonversi(
        TarifBarangType bom,
        IBrg brg,
        string satuanBomId,
        string satuanKecilId)
    {
        if (string.Equals(satuanBomId, satuanKecilId, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var matchingSatuan = brg.ListSatuan?
            .FirstOrDefault(s => string.Equals(
                (s.Satuan?.SatuanId ?? string.Empty).Trim(),
                satuanBomId,
                StringComparison.OrdinalIgnoreCase));

        if (matchingSatuan is not null && matchingSatuan.Konversi > 0)
            return matchingSatuan.Konversi;

        throw new InvalidOperationException(
            $"Konfigurasi satuan tidak sah: Satuan BOM '{bom.SatuanId}' pada barang '{brg.BrgName}' ({bom.BrgId}) tidak terdaftar pada satuan kecil ('{satuanKecilId}') maupun daftar konversi barang.");
    }

    private Dictionary<string, decimal> LoadAvailableStockByBrg(
        IEnumerable<ResolvedBomItem> resolvedItems,
        string layananId)
    {
        var availableStockByBrg = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in resolvedItems)
        {
            if (availableStockByBrg.ContainsKey(item.Bom.BrgId))
                continue;

            decimal total;
            try
            {
                total = _legacyStockReadPort.ListBalancesAtLocation(item.Bom.BrgId, layananId)
                    .Where(c => c.QtySisa > 0m)
                    .Sum(c => c.QtySisa);
            }
            catch
            {
                total = 0m;
            }

            availableStockByBrg[item.Bom.BrgId] = total;
        }

        return availableStockByBrg;
    }

    private record ResolvedBomItem(
        TarifBarangType Bom,
        string SatuanKecilId,
        decimal QtyBomTotal,
        decimal QtyKecilRequested);

    private record CalculatedItem(
        ResolvedBomItem Resolved,
        decimal QtyImmediateCut,
        decimal QtyPending);
}

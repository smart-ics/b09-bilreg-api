using Ardalis.GuardClauses;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public class TindakanBomProcessor : ITindakanBomProcessor
{
    private readonly ITarifRepo _tarifRepo;
    private readonly ITarifBarangRepo _tarifBarangRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly IStockBatchRepo _stockBatchRepo;
    private readonly IPakaiBrgInternalService _pakaiBrgInternalService;
    private readonly ITindakanBarangRepo _tindakanBarangRepo;
    private readonly ITindakanStokPendingRepo _tindakanStokPendingRepo;
    private readonly IBrgPricingService _brgPricingService;
    private readonly IMediator _mediator;
    private readonly ILegacyStockReadPort? _legacyStockReadPort;

    public TindakanBomProcessor(
        ITarifRepo tarifRepo,
        ITarifBarangRepo tarifBarangRepo,
        IBrgRepo brgRepo,
        IStockBatchRepo stockBatchRepo,
        IPakaiBrgInternalService pakaiBrgInternalService,
        ITindakanBarangRepo tindakanBarangRepo,
        ITindakanStokPendingRepo tindakanStokPendingRepo,
        IBrgPricingService brgPricingService,
        IMediator mediator,
        ILegacyStockReadPort? legacyStockReadPort = null)
    {
        _tarifRepo = tarifRepo;
        _tarifBarangRepo = tarifBarangRepo;
        _brgRepo = brgRepo;
        _stockBatchRepo = stockBatchRepo;
        _pakaiBrgInternalService = pakaiBrgInternalService;
        _tindakanBarangRepo = tindakanBarangRepo;
        _tindakanStokPendingRepo = tindakanStokPendingRepo;
        _brgPricingService = brgPricingService;
        _mediator = mediator;
        _legacyStockReadPort = legacyStockReadPort;
    }

    public async Task<TindakanBomResult> Process(
        TindakanModel tindakan,
        RegModel reg,
        string userId,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(tindakan, nameof(tindakan));
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var tarifId = (tindakan.Tarif?.TarifId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tarifId))
        {
            return new TindakanBomResult(tindakan.TindakanId, string.Empty, false, []);
        }

        // 1. Load Tarif and BOM items
        var isChargeBarang = _tarifRepo.LoadEntity(TarifType.Key(tarifId))
            .Match(t => t.IsChargeBarang, () => false);

        var bomList = _tarifBarangRepo.ListData(TarifType.Key(tarifId))?
            .OrderBy(x => x.NoUrut)
            .ToList() ?? [];

        if (bomList.Count == 0)
        {
            return new TindakanBomResult(tindakan.TindakanId, string.Empty, isChargeBarang, []);
        }

        var tindakanQty = tindakan.Qty <= 0 ? 1 : tindakan.Qty;
        var layananId = (tindakan.Layanan?.LayananId ?? string.Empty).Trim();
        var tipeJaminanId = (reg.TipeJaminan?.TipeJaminanId ?? string.Empty).Trim();

        // 2. Validate goods & convert UoM to SatuanKecil
        var resolvedItems = new List<ResolvedBomItem>();
        foreach (var bom in bomList)
        {
            var brgKey = new BrgKey(bom.BrgId);
            var brgMaybe = _brgRepo.LoadEntity(brgKey);
            if (!brgMaybe.HasValue)
            {
                throw new KeyNotFoundException($"Barang dengan Id '{bom.BrgId}' tidak ditemukan.");
            }
            var brg = brgMaybe.Value;

            var satuanBomId = (bom.SatuanId ?? string.Empty).Trim();
            var satuanKecilId = (brg.SatuanKecil?.SatuanId ?? string.Empty).Trim();

            decimal konversi;
            if (string.Equals(satuanBomId, satuanKecilId, StringComparison.OrdinalIgnoreCase))
            {
                konversi = 1m;
            }
            else
            {
                var matchingSatuan = brg.ListSatuan?
                    .FirstOrDefault(s => string.Equals((s.Satuan?.SatuanId ?? string.Empty).Trim(), satuanBomId, StringComparison.OrdinalIgnoreCase));

                if (matchingSatuan != null && matchingSatuan.Konversi > 0)
                {
                    konversi = matchingSatuan.Konversi;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Konfigurasi satuan tidak sah: Satuan BOM '{bom.SatuanId}' pada barang '{brg.BrgName}' ({bom.BrgId}) tidak terdaftar pada satuan kecil ('{satuanKecilId}') maupun daftar konversi barang.");
                }
            }

            var qtyBomTotal = bom.QtyBarang * tindakanQty;
            var qtyKecilRequested = qtyBomTotal * konversi;

            decimal hargaJualTotal = 0m;
            if (isChargeBarang && !string.IsNullOrWhiteSpace(bom.BrgId))
            {
                var priceResult = _brgPricingService.CalculateItemPrice(
                    bom.BrgId,
                    qtyKecilRequested,
                    tipeJaminanId,
                    layananId);
                hargaJualTotal = priceResult.Nilai.Total;
            }

            resolvedItems.Add(new ResolvedBomItem(
                bom,
                brg,
                satuanKecilId,
                konversi,
                qtyBomTotal,
                qtyKecilRequested,
                hargaJualTotal));
        }

        // 3. Freshness Gate
        var distinctBrgIds = resolvedItems
            .Select(x => x.Bom.BrgId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var allScopes = new List<ScopeKeyDto>();
        foreach (var brgId in distinctBrgIds)
        {
            var candidates = _stockBatchRepo.ListAllocationCandidates(brgId, layananId).ToList();
            var activeDos = _legacyStockReadPort?.ListActiveDoIdsAtLocation(brgId, layananId) ?? [];
            var scopes = candidates
                .Select(c => new ScopeKeyDto(brgId, c.BrgMasukReffId))
                .Concat(activeDos.Select(doId => new ScopeKeyDto(brgId, doId)))
                .Distinct();
            allScopes.AddRange(scopes);
        }

        var distinctScopes = allScopes.Distinct().ToList();
        if (distinctScopes.Count > 0)
        {
            var freshnessResult = await _mediator.Send(
                new EnsureFreshnessForScopesCommand(distinctScopes, userId),
                cancellationToken);

            if (freshnessResult.Outcome == EnsureFreshnessOutcomeEnum.AbortedInconsistent)
            {
                throw new InvalidOperationException(
                    $"Freshness gate gagal untuk barang '{freshnessResult.FailedBrgId}': {freshnessResult.InconsistencyReason}");
            }
        }

        // 4. Stock Allocation (Immediate Cut vs Pending)
        var availableStockByBrg = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in resolvedItems)
        {
            if (!availableStockByBrg.ContainsKey(item.Bom.BrgId))
            {
                var total = _stockBatchRepo.ListAllocationCandidates(item.Bom.BrgId, layananId).Sum(c => c.QtySisa);
                availableStockByBrg[item.Bom.BrgId] = total;
            }
        }

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

        // 5. Publish tb_trs_pakai via IPakaiBrgInternalService if any qtyImmediateCut >= 1
        var immediateCutItems = calculatedItems
            .Where(x => x.QtyImmediateCut >= 1m)
            .Select(x => new PakaiBrgCreateFromTindakanItem(
                BrgId: x.Resolved.Bom.BrgId,
                Qty: x.QtyImmediateCut))
            .ToList();

        string pakaiBrgId = string.Empty;
        if (immediateCutItems.Count > 0)
        {
            var pakaiCmd = new PakaiBrgCreateFromTindakanCommand(
                RegId: reg.RegId,
                LayananId: layananId,
                UserId: userId,
                KeteranganHeader: $"TDK:{tindakan.TindakanId} {tarifId}",
                ListItem: immediateCutItems);

            var pakaiResult = await _pakaiBrgInternalService.CreateFromTindakan(pakaiCmd, cancellationToken);
            pakaiBrgId = pakaiResult.PakaiBrgId;
        }

        // 6. Insert BILRG_TindakanBarang (all items) and BILRG_TindakanStokPending (only if qtyPending > 0)
        var listTindakanBarang = new List<TindakanBarangType>();
        var listPending = new List<TindakanStokPendingType>();
        var resultItems = new List<TindakanBomItemResult>();

        foreach (var calc in calculatedItems)
        {
            var tindakanBarangId = NunaId.New("TBG");
            var itemPakaiBrgId = calc.QtyImmediateCut >= 1m ? pakaiBrgId : string.Empty;
            var trsBillingId = isChargeBarang ? $"{tindakan.TindakanId}-B{calc.Resolved.Bom.NoUrut:D2}" : string.Empty;

            var tindakanBarang = new TindakanBarangType(
                TindakanBarangId: tindakanBarangId,
                TindakanId: tindakan.TindakanId,
                NoUrut: calc.Resolved.Bom.NoUrut,
                BrgId: calc.Resolved.Bom.BrgId,
                SatuanBomId: calc.Resolved.Bom.SatuanId,
                QtyBom: calc.Resolved.QtyBomTotal,
                SatuanKecilId: calc.Resolved.SatuanKecilId,
                QtyKecilRequested: calc.Resolved.QtyKecilRequested,
                QtyKecilDeducted: calc.QtyImmediateCut,
                QtyKecilPending: calc.QtyPending,
                IsChargeBarang: isChargeBarang,
                HargaJualTotal: calc.Resolved.HargaJualTotal,
                PakaiBrgId: itemPakaiBrgId,
                TrsBillingId: trsBillingId,
                IsVoid: false);

            listTindakanBarang.Add(tindakanBarang);

            if (calc.QtyPending > 0m)
            {
                var pendingId = NunaId.New("PND");
                var pending = new TindakanStokPendingType(
                    PendingId: pendingId,
                    TindakanId: tindakan.TindakanId,
                    TindakanBarangId: tindakanBarangId,
                    NoUrut: calc.Resolved.Bom.NoUrut,
                    RegId: reg.RegId,
                    LayananId: layananId,
                    BrgId: calc.Resolved.Bom.BrgId,
                    SatuanKecilId: calc.Resolved.SatuanKecilId,
                    QtyPending: calc.QtyPending,
                    QtySettled: 0m,
                    StatusPotStok: false,
                    PakaiBrgId: string.Empty,
                    IsVoid: false);

                listPending.Add(pending);
            }

            resultItems.Add(new TindakanBomItemResult(
                BrgId: calc.Resolved.Bom.BrgId,
                BrgName: calc.Resolved.Bom.BrgName,
                NoUrut: calc.Resolved.Bom.NoUrut,
                SatuanBomId: calc.Resolved.Bom.SatuanId,
                QtyBom: calc.Resolved.QtyBomTotal,
                Konversi: calc.Resolved.Konversi,
                SatuanKecilId: calc.Resolved.SatuanKecilId,
                QtyKecilRequested: calc.Resolved.QtyKecilRequested,
                QtyImmediateCut: calc.QtyImmediateCut,
                QtyPending: calc.QtyPending,
                PakaiBrgId: itemPakaiBrgId,
                IsChargeBarang: isChargeBarang,
                HargaJualTotal: calc.Resolved.HargaJualTotal,
                TindakanBarangId: tindakanBarangId,
                QtyPerTindakan: calc.Resolved.Bom.QtyBarang));
        }

        _tindakanBarangRepo.Insert(listTindakanBarang, userId);

        if (listPending.Count > 0)
        {
            _tindakanStokPendingRepo.Insert(listPending, userId);
        }

        return new TindakanBomResult(
            TindakanId: tindakan.TindakanId,
            PakaiBrgId: pakaiBrgId,
            IsChargeBarang: isChargeBarang,
            Items: resultItems);
    }

    private record ResolvedBomItem(
        TarifBarangType Bom,
        IBrg Brg,
        string SatuanKecilId,
        decimal Konversi,
        decimal QtyBomTotal,
        decimal QtyKecilRequested,
        decimal HargaJualTotal);

    private record CalculatedItem(
        ResolvedBomItem Resolved,
        decimal QtyImmediateCut,
        decimal QtyPending);
}

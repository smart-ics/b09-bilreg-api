using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Microsoft.Extensions.Logging;

namespace Bilreg.Application.IgdContext.IgdVisitFeature;

public class IgdBillingPostingOrchestrator : IIgdBillingPostingOrchestrator
{
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ITindakanIgdRepo _tindakanIgdRepo;
    private readonly IBhpIgdRepo _bhpIgdRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly IGroupRekRepo _groupRekRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly ILogger<IgdBillingPostingOrchestrator> _logger;
    private readonly IBrgPricingService? _brgPricingService;

    public IgdBillingPostingOrchestrator(
        INilaiTarifRepo nilaiTarifRepo,
        ITarifRepo tarifRepo,
        IKomponenRepo komponenRepo,
        IJaminanRepo jaminanRepo,
        IPpaRepo ppaRepo,
        ITindakanIgdRepo tindakanIgdRepo,
        IBhpIgdRepo bhpIgdRepo,
        IBrgRepo brgRepo,
        IGroupRekRepo groupRekRepo,
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        ILogger<IgdBillingPostingOrchestrator> logger,
        IBrgPricingService? brgPricingService = null)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _tarifRepo = tarifRepo;
        _komponenRepo = komponenRepo;
        _jaminanRepo = jaminanRepo;
        _ppaRepo = ppaRepo;
        _tindakanIgdRepo = tindakanIgdRepo;
        _bhpIgdRepo = bhpIgdRepo;
        _brgRepo = brgRepo;
        _groupRekRepo = groupRekRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _logger = logger;
        _brgPricingService = brgPricingService;
    }

    public async Task<bool> PostTindakanAsync(
        IgdVisitModel visit,
        RegModel reg,
        TindakanIgdModel tindakan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentNullException.ThrowIfNull(reg);
        ArgumentNullException.ThrowIfNull(tindakan);

        if (tindakan.IsPosted)
            return true;

        try
        {
            var expectedJaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
                ? reg.TipeJaminan.TipeJaminanId[..3]
                : reg.TipeJaminan.TipeJaminanId;
            var jaminan = _jaminanRepo.LoadEntity(JaminanType.Key(expectedJaminanId))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"Jaminan '{expectedJaminanId}' not found")
                );

            var occurredAt = tindakan.Audit.Timestamp == default ? DateTime.Now : tindakan.Audit.Timestamp;
            var userId = string.IsNullOrWhiteSpace(tindakan.Audit.UserId) ? "SYSTEM" : tindakan.Audit.UserId;

            if (tindakan.Aktifitas == ActivityTindakanIgd.Barang)
            {
                var subtotal = _brgPricingService != null
                    ? _brgPricingService.CalculateItemPrice(
                        tindakan.ReffId,
                        tindakan.Qty,
                        reg.TipeJaminan.TipeJaminanId,
                        reg.Layanan.LayananId).Nilai.Total
                    : 0m;

                var brgBill = SaveBarangBill(
                    reg,
                    jaminan,
                    tindakan.ReffId,
                    tindakan.Descriptions,
                    tindakan.Qty,
                    subtotal,
                    tindakan.TindakanIgdId,
                    occurredAt,
                    userId);

                tindakan.MarkAsPosted(tindakan.TindakanIgdId, brgBill.TrsBillingId, occurredAt);
                _tindakanIgdRepo.SaveChanges(tindakan);

                return await Task.FromResult(true);
            }

            var tarifKey = TarifType.Key(tindakan.ReffId);
            var tarif = _tarifRepo.LoadEntity(tarifKey)
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"Tarif '{tarifKey.TarifId}' not found")
                );

            var tipeTarifReff = jaminan.TipeTarif.Rajal;
            var kelasKey = reg.Kelas;
            var nilaiTarifKey = NilaiTarifType.KeyComposite(tarifKey, tipeTarifReff, kelasKey);
            var nilaiTarif = _nilaiTarifRepo.LoadEntity(nilaiTarifKey)
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException(
                        $"NilaiTarif not found for Tarif '{tarifKey.TarifId}', TipeTarif '{tipeTarifReff.TipeTarifId}', Kelas '{kelasKey.KelasId}'")
                );

            var listKomp = _komponenRepo.ListData(nilaiTarif.ListKomponen.Select(x => x.Komponen))?.ToList() ?? [];

            var ppa = _ppaRepo.LoadEntity(PpaType.Key(tindakan.Ppa.PpaId))
                .Match(
                    onSome: x => x,
                    onNone: () => PpaType.Default with { PpaId = tindakan.Ppa.PpaId, PpaName = tindakan.Ppa.PpaName }
                );

            var bill = SaveTindakanBill(
                reg,
                jaminan,
                tarif,
                nilaiTarif,
                listKomp,
                ppa,
                tindakan.ListPpa,
                tindakan.Qty,
                tindakan.TindakanIgdId,
                occurredAt,
                userId);

            if (tindakan.ListBarang.Count > 0)
            {
                var updatedBarang = new List<TindakanIgdBarangType>(tindakan.ListBarang.Count);
                var hasBomBill = false;

                foreach (var barang in tindakan.ListBarang)
                {
                    if (!barang.IsVoid && barang.IsChargeBarang)
                    {
                        var bomBillId = $"{tindakan.TindakanIgdId}-B{barang.NoUrut:D2}";
                        var subtotal = _brgPricingService != null
                            ? _brgPricingService.CalculateItemPrice(
                                barang.BrgId,
                                barang.QtyKecilRequested,
                                reg.TipeJaminan.TipeJaminanId,
                                reg.Layanan.LayananId).Nilai.Total
                            : 0m;

                        var brgName = _brgRepo.LoadEntity(new BrgKey(barang.BrgId))
                            .Match(onSome: b => b.BrgName, onNone: () => barang.BrgId);

                        SaveBarangBill(
                            reg,
                            jaminan,
                            barang.BrgId,
                            brgName,
                            barang.QtyKecilRequested,
                            subtotal,
                            bomBillId,
                            occurredAt,
                            userId);

                        updatedBarang.Add(barang with { TrsBillingId = bomBillId });
                        hasBomBill = true;
                    }
                    else
                    {
                        updatedBarang.Add(barang);
                    }
                }

                if (hasBomBill)
                    tindakan.SetListBarang(updatedBarang);
            }

            tindakan.MarkAsPosted(tindakan.TindakanIgdId, bill.TrsBillingId, occurredAt);
            _tindakanIgdRepo.SaveChanges(tindakan);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post TindakanIgd '{TindakanIgdId}' to billing for visit '{IgdVisitId}'.",
                tindakan.TindakanIgdId, visit.IgdVisitId);
            return false;
        }
    }

    public async Task<bool> PostBhpAsync(
        IgdVisitModel visit,
        RegModel reg,
        BhpIgdModel bhp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentNullException.ThrowIfNull(reg);
        ArgumentNullException.ThrowIfNull(bhp);

        if (bhp.IsPosted)
            return true;

        try
        {
            var expectedJaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
                ? reg.TipeJaminan.TipeJaminanId[..3]
                : reg.TipeJaminan.TipeJaminanId;
            var jaminan = _jaminanRepo.LoadEntity(JaminanType.Key(expectedJaminanId))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"Jaminan '{expectedJaminanId}' not found")
                );

            var occurredAt = bhp.Audit.Timestamp == default ? DateTime.Now : bhp.Audit.Timestamp;
            var userId = string.IsNullOrWhiteSpace(bhp.Audit.UserId) ? "SYSTEM" : bhp.Audit.UserId;

            var bill = SaveBarangBill(
                reg,
                jaminan,
                bhp.BhpItemId,
                bhp.BhpItemName,
                bhp.Qty,
                bhp.Subtotal,
                bhp.BhpIgdId,
                occurredAt,
                userId);

            bhp.MarkAsPosted(bill.TrsBillingId, occurredAt);
            _bhpIgdRepo.SaveChanges(bhp);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post BhpIgd '{BhpIgdId}' to billing for visit '{IgdVisitId}'.",
                bhp.BhpIgdId, visit.IgdVisitId);
            return false;
        }
    }

    private TrsBillType SaveBarangBill(
        RegModel reg,
        JaminanType jaminan,
        string brgId,
        string itemName,
        decimal qty,
        decimal subtotal,
        string billId,
        DateTime occurredAt,
        string userId)
    {
        var brg = _brgRepo.LoadEntity(new BrgKey(brgId));
        var groupRek = GroupRekType.Default;
        if (brg.HasValue)
        {
            var groupRekKey = GroupRekType.Key(brg.Value.GroupRek.GroupRekId);
            groupRek = _groupRekRepo.LoadEntity(groupRekKey).GetValueOrDefault(GroupRekType.Default);
        }

        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpObatRanap
            : jaminan.Rekening.PpdpObatRajal;

        var rekPdpt = (reg.JenisReg != JenisRegEnum.RegInap && !string.IsNullOrWhiteSpace(groupRek.RekJualRajal.CoaId) && groupRek.RekJualRajal.CoaId != "-")
            ? groupRek.RekJualRajal
            : (!string.IsNullOrWhiteSpace(groupRek.RekJual.CoaId) && groupRek.RekJual.CoaId != "-")
                ? groupRek.RekJual
                : (reg.JenisReg == JenisRegEnum.RegInap ? jaminan.Rekening.PpdpObatRanap : jaminan.Rekening.PpdpObatRajal);

        var bill2Coa = new TrsBill2CoaType(
            rekPpdp,
            rekPdpt,
            groupRek.RekPersediaan,
            CoaType.Default,
            CoaType.Default,
            groupRek.RekDisc);

        var groupRekId = groupRek.GroupRekId != "-" ? groupRek.GroupRekId : "BHP";
        var groupRekName = groupRek.GroupRekName != "-" ? groupRek.GroupRekName : "BHP";
        var komponen = new TrsBill2KomponenType(groupRekId, groupRekName);

        var ppa = reg.Dokter;
        var bill2Event = new TrsBill2TransEventType(
            0,
            komponen,
            TrsBillJenisBayarType.Pdp,
            subtotal,
            ppa,
            bill2Coa);

        var audit = AuditTrailType.Create(userId, occurredAt);

        var ketBilling = new TrsBillKetType(
            itemName,
            "",
            brgId,
            qty,
            billId);

        var nilai = new TrsBillNilaiType(subtotal, 0, 0, 0);
        var rekapCetak = new RekapCetakReff(groupRek.RekapCetakId != "-" ? groupRek.RekapCetakId : string.Empty, "");

        var bill = new TrsBillType(
            billId,
            BillModulGroup.Obat,
            occurredAt,
            reg.ToReff(),
            reg.Layanan,
            reg.Kelas,
            audit.Created,
            rekapCetak,
            nilai,
            ketBilling,
            [bill2Event],
            [],
            []);

        var isNewTataRekening = false;
        var tataRekening = _tataRekeningRepo.LoadEntity(reg).Match(
            onSome: m => m,
            onNone: () =>
            {
                isNewTataRekening = true;
                return TataRekeningModel.Create(reg.RegId);
            });
        if (isNewTataRekening)
            _tataRekeningRepo.SaveChanges(tataRekening);

        _trsBillingRepo.SaveChanges(bill);
        return bill;
    }

    private TrsBillType SaveTindakanBill(
        RegModel reg,
        JaminanType jaminan,
        TarifType tarif,
        NilaiTarifType nilaiTarif,
        IEnumerable<KomponenType> listReffKomp,
        PpaType ppa,
        IReadOnlyList<TindakanIgdPpaType> listPpa,
        decimal qty,
        string billId,
        DateTime occurredAt,
        string userId)
    {
        var rekPpdp = reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.Rekening.PpdpJasaRanap
            : jaminan.Rekening.PpdpJasaRajal;

        var ppaByKomponen = new Dictionary<string, TindakanIgdPpaType>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in listPpa)
        {
            if (!string.IsNullOrWhiteSpace(entry.KomponenId) && !ppaByKomponen.ContainsKey(entry.KomponenId))
                ppaByKomponen[entry.KomponenId] = entry;
        }

        var listReffKompFetched = listReffKomp.ToList();
        var listBill2TransEvent = new List<TrsBill2TransEventType>();
        var i = 0;

        foreach (var item in nilaiTarif.ListKomponen)
        {
            var reffKomp = listReffKompFetched.FirstOrDefault(x => x.KomponenId == item.Komponen.KomponenId);

            var rekPdpt = (reffKomp != null && !string.IsNullOrWhiteSpace(reffKomp.RekPdpt.CoaId) && reffKomp.RekPdpt.CoaId != "-")
                ? reffKomp.RekPdpt
                : rekPpdp;

            var rekDiskon = (reffKomp != null && !string.IsNullOrWhiteSpace(reffKomp.RekDiskon.CoaId) && reffKomp.RekDiskon.CoaId != "-")
                ? reffKomp.RekDiskon
                : CoaType.Default;

            var bill2Coa = new TrsBill2CoaType(
                rekPpdp,
                rekPdpt,
                CoaType.Default,
                CoaType.Default,
                CoaType.Default,
                rekDiskon);

            var komponen = new TrsBill2KomponenType(item.Komponen.KomponenId, item.Komponen.KomponenName);
            var itemSubtotal = item.Nilai * qty;
            var komponenPpa = ResolveKomponenPpa(item.Komponen.KomponenId, ppaByKomponen, ppa);

            var bill2Event = new TrsBill2TransEventType(
                i++,
                komponen,
                TrsBillJenisBayarType.Pdp,
                itemSubtotal,
                komponenPpa,
                bill2Coa);

            listBill2TransEvent.Add(bill2Event);
        }

        var totalSubtotal = nilaiTarif.Nilai * qty;
        var audit = AuditTrailType.Create(userId, occurredAt);

        var ketBilling = new TrsBillKetType(
            tarif.TarifName,
            "",
            tarif.TarifId,
            qty,
            billId);

        var nilai = new TrsBillNilaiType(totalSubtotal, 0, 0, 0);
        var rekapCetak = tarif.RekapCetak;

        var bill = new TrsBillType(
            billId,
            BillModulGroup.Jasa,
            occurredAt,
            reg.ToReff(),
            reg.Layanan,
            reg.Kelas,
            audit.Created,
            rekapCetak,
            nilai,
            ketBilling,
            listBill2TransEvent,
            [],
            []);

        var isNewTataRekening = false;
        var tataRekening = _tataRekeningRepo.LoadEntity(reg).Match(
            onSome: m => m,
            onNone: () =>
            {
                isNewTataRekening = true;
                return TataRekeningModel.Create(reg.RegId);
            });
        if (isNewTataRekening)
            _tataRekeningRepo.SaveChanges(tataRekening);

        _trsBillingRepo.SaveChanges(bill);
        return bill;
    }

    private PpaReff ResolveKomponenPpa(
        string komponenId,
        IReadOnlyDictionary<string, TindakanIgdPpaType> ppaByKomponen,
        PpaType fallbackPpa)
    {
        if (!ppaByKomponen.TryGetValue(komponenId, out var ppaEntry) || string.IsNullOrWhiteSpace(ppaEntry.PpaId))
            return fallbackPpa.ToReff();

        if (!string.IsNullOrWhiteSpace(ppaEntry.PpaName))
            return new PpaReff(ppaEntry.PpaId, ppaEntry.PpaName);

        return _ppaRepo.LoadEntity(PpaType.Key(ppaEntry.PpaId))
            .Match(
                onSome: x => x.ToReff(),
                onNone: () => new PpaReff(ppaEntry.PpaId, ppaEntry.PpaName));
    }

    public async Task<bool> SyncAllPendingForVisitAsync(
        IgdVisitModel visit,
        RegModel reg,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentNullException.ThrowIfNull(reg);

        var allSuccess = true;

        var pendingTindakanViews = _tindakanIgdRepo.ListData(visit)
            .Where(x => !x.IsPosted)
            .ToList();

        foreach (var view in pendingTindakanViews)
        {
            var tindakanOpt = _tindakanIgdRepo.LoadEntity(TindakanIgdModel.Key(view.TindakanIgdId));
            if (tindakanOpt.HasValue)
            {
                var success = await PostTindakanAsync(visit, reg, tindakanOpt.Value, cancellationToken);
                if (!success)
                    allSuccess = false;
            }
        }

        var pendingBhpViews = _bhpIgdRepo.ListData(visit)
            .Where(x => !x.IsPosted)
            .ToList();

        foreach (var view in pendingBhpViews)
        {
            var bhpOpt = _bhpIgdRepo.LoadEntity(BhpIgdModel.Key(view.BhpIgdId));
            if (bhpOpt.HasValue)
            {
                var success = await PostBhpAsync(visit, reg, bhpOpt.Value, cancellationToken);
                if (!success)
                    allSuccess = false;
            }
        }

        return allSuccess;
    }
}

using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BrgContext.BrgFeature;
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
using Bilreg.Domain.ChargeContext.TindakanFeature;
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
    private readonly ITindakanRepo _tindakanRepo;
    private readonly ITindakanIgdRepo _tindakanIgdRepo;
    private readonly IBhpIgdRepo _bhpIgdRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly IGroupRekRepo _groupRekRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly ILogger<IgdBillingPostingOrchestrator> _logger;

    public IgdBillingPostingOrchestrator(
        INilaiTarifRepo nilaiTarifRepo,
        ITarifRepo tarifRepo,
        IKomponenRepo komponenRepo,
        IJaminanRepo jaminanRepo,
        IPpaRepo ppaRepo,
        ITindakanRepo tindakanRepo,
        ITindakanIgdRepo tindakanIgdRepo,
        IBhpIgdRepo bhpIgdRepo,
        IBrgRepo brgRepo,
        IGroupRekRepo groupRekRepo,
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IAddBillAppService addBillAppService,
        ILogger<IgdBillingPostingOrchestrator> logger)
    {
        _nilaiTarifRepo = nilaiTarifRepo;
        _tarifRepo = tarifRepo;
        _komponenRepo = komponenRepo;
        _jaminanRepo = jaminanRepo;
        _ppaRepo = ppaRepo;
        _tindakanRepo = tindakanRepo;
        _tindakanIgdRepo = tindakanIgdRepo;
        _bhpIgdRepo = bhpIgdRepo;
        _brgRepo = brgRepo;
        _groupRekRepo = groupRekRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _addBillAppService = addBillAppService;
        _logger = logger;
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

            var listPpa = listKomp
                .Where(x => x.ListSatTugas.Any())
                .Select(x => new KomponenPpaView(x, ppa))
                .ToList();

            var occurredAt = tindakan.Audit.Timestamp == default ? DateTime.Now : tindakan.Audit.Timestamp;
            var userId = string.IsNullOrWhiteSpace(tindakan.Audit.UserId) ? "SYSTEM" : tindakan.Audit.UserId;
            var bedId = (visit.BedId == "-" || string.IsNullOrWhiteSpace(visit.BedId)) ? string.Empty : visit.BedId;

            var tdk = TindakanModel.FromReg(
                reg,
                nilaiTarif,
                listPpa,
                userId,
                occurredAt,
                pakaiBedId: bedId,
                qty: tindakan.Qty);

            _tindakanRepo.SaveChanges(tdk);

            var bill = _addBillAppService.FromTindakan(tdk, reg, tarif, jaminan, listKomp, occurredAt);

            tindakan.MarkAsPosted(tdk.TindakanId, bill.TrsBillingId, occurredAt);
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

            var brg = _brgRepo.LoadEntity(new BrgKey(bhp.BhpItemId));
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
                bhp.Subtotal,
                ppa,
                bill2Coa);

            var occurredAt = bhp.Audit.Timestamp == default ? DateTime.Now : bhp.Audit.Timestamp;
            var userId = string.IsNullOrWhiteSpace(bhp.Audit.UserId) ? "SYSTEM" : bhp.Audit.UserId;
            var audit = AuditTrailType.Create(userId, occurredAt);

            var ketBilling = new TrsBillKetType(
                bhp.BhpItemName,
                "",
                bhp.BhpItemId,
                bhp.Qty,
                bhp.BhpIgdId);

            var nilai = new TrsBillNilaiType(bhp.Subtotal, 0, 0, 0);
            var rekapCetak = new RekapCetakReff(groupRek.RekapCetakId != "-" ? groupRek.RekapCetakId : string.Empty, "");

            var bill = new TrsBillType(
                bhp.BhpIgdId,
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

using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TdkCreateTindakanByOrderCmd(string RegId, string TarifId,
    string DokterId, string UserId)
    : IRequest<TdkCreateTindakanByOrderResponse>, IRegKey, ITarifKey;

public record TdkCreateTindakanByOrderResponse(string TindakanId);

public class TdkCreateTindakanByOrderHandler : IRequestHandler<TdkCreateTindakanByOrderCmd, TdkCreateTindakanByOrderResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly ITglJamProvider _tglJamProvider;
    public TdkCreateTindakanByOrderHandler(IRegRepo regRepo, IJaminanRepo jaminanRepo,
        IPpaRepo ppaRepo, ILayananRepo layananRepo, ITarifRepo tarifRepo,
        INilaiTarifRepo nilaiTarifRepo, IKomponenRepo komponenRepo,
        IMapJaminanJkRepo mapJaminanJkRepo, ITindakanRepo tindakanRepo,
        ITrsBillingRepo trsBillingRepo, IJurnalRepo jurnalRepo,
        IAddBillAppService addBillAppService, ITglJamProvider tglJamProvider)
    {
        _regRepo = regRepo;
        _jaminanRepo = jaminanRepo;
        _ppaRepo = ppaRepo;
        _layananRepo = layananRepo;
        _tarifRepo = tarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _tindakanRepo = tindakanRepo;
        _trsBillingRepo = trsBillingRepo;
        _jurnalRepo = jurnalRepo;
        _addBillAppService = addBillAppService;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TdkCreateTindakanByOrderResponse> Handle(TdkCreateTindakanByOrderCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.TarifId);
        Guard.Against.NullOrWhiteSpace(request.DokterId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        //  BUILD
        var reg = LoadReg(request);
        var jaminan = LoadJaminan(reg);
        var dokter = LoadDokter(request);
        var layanan = LoadLayanan(reg);
        var tarif = LoadTarif(request);

        var tipeTarif = ResolveTipeTarif(reg, jaminan);
        var nilaiTarif = LoadNilaiTarif(request, tipeTarif, reg);

        var listKomp = _komponenRepo
            .ListData(nilaiTarif.ListKomponen.Select(x => x.Komponen))?.ToList() ?? [];
        var listPpa = listKomp
            .Where(x => x.ListSatTugas.Any(t => t.IsMedis))
            .Select(x => new KomponenPpaView(x, dokter));

        var occurredAt = _tglJamProvider.Now;
        var tindakan = TindakanModel.FromReg(reg, nilaiTarif, listPpa,
            request.UserId, occurredAt);
        var trsBilling = _addBillAppService.FromTindakan(tindakan, reg, tarif,
            jaminan, listKomp, occurredAt);
        var mapJaminanJk = LoadMapJmnJk(jaminan);
        var jurnal = JurnalType.CreateFromTrsBilling(trsBilling, layanan, mapJaminanJk);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _tindakanRepo.SaveChanges(tindakan);
        _trsBillingRepo.SaveChanges(trsBilling);
        _jurnalRepo.SaveChanges(jurnal);
        trans.Complete();

        //  RESPONSE
        return Task.FromResult(new TdkCreateTindakanByOrderResponse(
            tindakan.TindakanId));
    }

    #region PRIVATE-HELPER
    private RegModel LoadReg(IRegKey key)
    {
        var result = _regRepo.LoadEntity(key)
            .GetValueOrThrow($"Register {key.RegId} not found");
        return !result.IsAktif
            ? throw new ArgumentException($"Register '{key.RegId}' tidak aktif")
            : result;
    }

    private JaminanType LoadJaminan(RegModel reg)
    {
        var jaminanKey = JaminanType.Key(reg.TipeJaminan.TipeJaminanId[..3]);
        return _jaminanRepo.LoadEntity(jaminanKey)
            .GetValueOrThrow($"Jaminan {reg.TipeJaminan.TipeJaminanId} not found");
    }

    private PpaType LoadDokter(TdkCreateTindakanByOrderCmd request) =>
        _ppaRepo.LoadEntity(PpaType.Key(request.DokterId))
            .GetValueOrThrow($"Dokter '{request.DokterId}' not found");

    private LayananType LoadLayanan(RegModel reg) =>
        _layananRepo.LoadEntity(LayananType.Key(reg.Layanan.LayananId))
            .GetValueOrThrow($"Layanan {reg.Layanan.LayananId} not found");

    private TarifType LoadTarif(ITarifKey key) =>
        _tarifRepo.LoadEntity(key)
            .GetValueOrThrow($"Tarif '{key.TarifId}' not found");

    private static TipeTarifReff ResolveTipeTarif(RegModel reg, JaminanType jaminan) =>
        reg.JenisReg == JenisRegEnum.RegInap
            ? jaminan.TipeTarif.Ranap
            : jaminan.TipeTarif.Rajal;

    private NilaiTarifType LoadNilaiTarif(ITarifKey tarifKey,
        TipeTarifReff tipeTarif, RegModel reg)
    {
        var compositKey = NilaiTarifType.KeyComposite(tarifKey, tipeTarif, reg.Kelas);
        return _nilaiTarifRepo.LoadEntity(compositKey)
            .GetValueOrThrow(
                $"NilaiTarif untuk Tarif '{tarifKey.TarifId}', " +
                $"TipeTarif '{tipeTarif.TipeTarifId}', " +
                $"Kelas '{reg.Kelas.KelasId}' tidak ditemukan");
    }

    private MapJaminanJkType LoadMapJmnJk(IJaminanKey key) =>
        _mapJaminanJkRepo.LoadEntity(key)
            .GetValueOrDefault(MapJaminanJkType.Default);
    #endregion
}
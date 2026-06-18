using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record TdkCreateTindakanCmd(string RegId,
    string LayananId, string NilaiTarifId, string UserId,
    IEnumerable<TdkCreateTindakanPpaCmd> ListPpa) 
    : IRequest<TindakanCreateRespose>, IRegKey, ILayananKey, INilaiTarifKey;

public record TdkCreateTindakanPpaCmd(string KomponenId, string PpaId);

public record TindakanCreateRespose(string TindakanId);

public class TindakanCreateHandler : IRequestHandler<TdkCreateTindakanCmd, TindakanCreateRespose>
{
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAddBillAppService _addBillAppService;

    public TindakanCreateHandler(ITindakanRepo tindakanRepo, 
        IRegRepo regRepo, ILayananRepo layananRepo, INilaiTarifRepo nilaiTarifRepo, 
        IKomponenRepo komponenRepo, IPpaRepo ppaRepo,  
        ITarifRepo tarifRepo, IJaminanRepo jaminanRepo,
        ITrsBillingRepo trsBillingRepo,
        IAddBillAppService addBillAppService)
    {
        _tindakanRepo = tindakanRepo;
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _ppaRepo = ppaRepo;
        _tarifRepo = tarifRepo;
        _jaminanRepo = jaminanRepo;
        _trsBillingRepo = trsBillingRepo;
        _addBillAppService = addBillAppService;
    }

    public Task<TindakanCreateRespose> Handle(TdkCreateTindakanCmd request, CancellationToken cancellationToken)
    {
        //  BUILD
        var reg = LoadReg(request);
        var layanan = LoadLayanan(request);
        var nilaiTarif = LoadNilaiTaif(request);
        var tarif = LoadTarif(nilaiTarif);
        var jaminanKey = reg.TipeJaminan.TipeJaminanId[..3];
        var jaminan = LoadJaminan(JaminanType.Key(jaminanKey));
        var listKomp = new List<KomponenType>();
        var listPpa = new List<KomponenPpaView>();
        foreach (var item in request.ListPpa)
        {
            var komp = LoadKomponen(KomponenType.Key(item.KomponenId));
            var ppa =  LoadPpa(PpaType.Key(item.PpaId));
            listPpa.Add( new KomponenPpaView(komp, ppa));
            listKomp.Add(komp);
        }

        var tindakan = TindakanModel.Create(reg, layanan, nilaiTarif, listPpa, request.UserId);
        var trsBilling = _addBillAppService.FromTindakan(tindakan, reg, tarif, jaminan, listKomp);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _tindakanRepo.SaveChanges(tindakan);
        _trsBillingRepo.SaveChanges(trsBilling);
        trans.Complete();

        //  RESPONSE
        return Task.FromResult(new TindakanCreateRespose(tindakan.TindakanId));
    }

    #region PRIVATE-HELPER
    private RegModel LoadReg(IRegKey key)
    {
        var result = _regRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register {key.RegId} not found")
            );

        return !result.IsAktif 
            ? throw new ArgumentException($"Register '{key.RegId}' tidak aktif") 
            : result;
    }

    private LayananType LoadLayanan(ILayananKey lynKey)
    {
        var layanan = _layananRepo.LoadEntity(lynKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan {lynKey.LayananId} not found")
            );
        return layanan;  
    }
    
    private NilaiTarifType LoadNilaiTaif(INilaiTarifKey nilaiKey)
    {
        var nilaiTarif = _nilaiTarifRepo.LoadEntity(nilaiKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Nilai Tarif invalid")
            );
        return nilaiTarif;
    }

    private TarifType LoadTarif(ITarifKey tarifKey)
    {
        var tarif = _tarifRepo.LoadEntity(tarifKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tarif invalid")
            );
        return tarif;
    }

    private JaminanType LoadJaminan(IJaminanKey jaminan)
    {
        var jmn = _jaminanRepo.LoadEntity(jaminan)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Jaminan invalid")
            );
        return jmn;
    }

    private KomponenType LoadKomponen(IKomponenKey key)
    {
        var komponen = _komponenRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Komponen Nilai Tarif '{key.KomponenId}' invalid")
            );
        return komponen;
    }
    private PpaType LoadPpa(IPpaKey key)
    {
        var ppa = _ppaRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"PPA '{key.PpaId}' invalid")
            );
        return ppa;
    }

    #endregion
}
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

public record TdkSaveTindakanCmd(string TindakanId, string RegId,
    string LayananId, string NilaiTarifId, string UserId,
    IEnumerable<TdkSaveTindakanPpaCmd> ListPpa)
    : IRequest<TdkSaveTindakanRespose>, ITindakanKey, IRegKey, ILayananKey, INilaiTarifKey;

public record TdkSaveTindakanPpaCmd(string KomponenId, string PpaId);
public record TdkSaveTindakanRespose(string TindakanId);

public class TdkSaveTindakanHandler : IRequestHandler<TdkSaveTindakanCmd, TdkSaveTindakanRespose>
{
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;
    public TdkSaveTindakanHandler(IRegRepo regRepo,
        ILayananRepo layananRepo,
        ITarifRepo tarifRepo,
        IJaminanRepo jaminanRepo,
        INilaiTarifRepo nilaiTarifRepo,
        IKomponenRepo komponenRepo,
        IPpaRepo ppaRepo,
        ITindakanRepo tindakanRepo,
        ITrsBillingRepo trsBillingRepo,
        IAddBillAppService addBillAppService,
        IMapJaminanJkRepo mapJaminanJkRepo,
        IJurnalRepo jurnalRepo)
    {
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _tarifRepo = tarifRepo;
        _jaminanRepo = jaminanRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _ppaRepo = ppaRepo;
        _tindakanRepo = tindakanRepo;
        _trsBillingRepo = trsBillingRepo;
        _addBillAppService = addBillAppService;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _jurnalRepo = jurnalRepo;
    }

    public Task<TdkSaveTindakanRespose> Handle(TdkSaveTindakanCmd request, CancellationToken cancellationToken)
    {
        //  BUILD
        if (request.ListPpa is null)
            throw new ArgumentException("List PPA tidak boleh null");

        var tindakan = _tindakanRepo.LoadEntity(request).GetValueOrDefault(TindakanModel.Default);
        var reg = LoadReg(request);
        var layanan = LoadLayanan(request);
        var nilaiTarif = LoadNilaiTaif(request);
        var tarif = LoadTarif(nilaiTarif);
        var jaminanKey = reg.TipeJaminan.TipeJaminanId[..3];
        var jaminan = LoadJaminan(JaminanType.Key(jaminanKey));
        
        var listKomp = ListKomponenTarif(nilaiTarif.ListKomponen.Select(x => x.Komponen));
        var listPpa = new List<KomponenPpaView>();
        foreach (var item in request.ListPpa)
        {
            var komp = listKomp.First(x => x.KomponenId == item.KomponenId);
            var ppa = LoadPpa(PpaType.Key(item.PpaId));
            listPpa.Add(new KomponenPpaView(komp, ppa));
        }

        var tdk = CreateOrEdit(tindakan, reg, layanan, nilaiTarif, listPpa, request.UserId);
        var trsBilling = _addBillAppService.FromTindakan(tdk, reg, tarif, jaminan, listKomp);
        var mapJaminanJk = LoadMapJmnJk(jaminan);
        var jurnal = tdk == TindakanModel.Default
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(trsBilling,
                layanan, mapJaminanJk);
        //  WRITE
        using var trans = TransHelper.NewScope();
        _tindakanRepo.SaveChanges(tdk);
        _trsBillingRepo.SaveChanges(trsBilling);
        _jurnalRepo.SaveChanges(jurnal);
        trans.Complete();

        //  RESPONSE
        return Task.FromResult(new TdkSaveTindakanRespose(tdk.TindakanId));
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
    private IEnumerable<KomponenType> ListKomponenTarif(IEnumerable<IKomponenKey> listKey)
    {
        var result = _komponenRepo
            .ListData(listKey)?.ToList() ?? [];
        return result;
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

    private MapJaminanJkType LoadMapJmnJk(IJaminanKey key)
    {
        var map = _mapJaminanJkRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => MapJaminanJkType.Default
            );
        return map;
    }

    private TindakanModel CreateOrEdit(
        TindakanModel tdk, RegModel reg, LayananType lyn,
        NilaiTarifType nilaiTarif, List<KomponenPpaView> listPpa, string userId)
    {
        if (tdk.TindakanId == "-")
            return TindakanModel.Create(reg, lyn, nilaiTarif, listPpa, userId);

        tdk.AuditTrail.Modif(userId, DateTime.Now);

        return TindakanModel.Save(
            tdk.TindakanId, reg, lyn, nilaiTarif, listPpa, tdk.AuditTrail
        );
    }

    #endregion
}

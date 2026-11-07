using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.BillingLockFeature;
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

using System.Text.Json.Serialization;

[method: JsonConstructor]
public record TdkSaveTindakanCmd(string TindakanId, string RegId,
    string LayananId, string NilaiTarifId, string UserId,
    IEnumerable<TdkSaveTindakanPpaCmd> ListPpa,
    string PakaiBedId = "", int Qty = 1,
    bool IsDiskonDokter = false,
    IEnumerable<TdkSaveTindakanKomponenDiskonCmd>? ListKomponenDiskon = null)
    : IRequest<TdkSaveTindakanRespose>, ITindakanKey, IRegKey, ILayananKey, INilaiTarifKey
{
    public TdkSaveTindakanCmd(string tindakanId, string regId, string layananId, string nilaiTarifId, string userId,
        IEnumerable<TdkSaveTindakanPpaCmd> listPpa, int qty, bool isDiskonDokter = false)
        : this(tindakanId, regId, layananId, nilaiTarifId, userId, listPpa, "", qty, isDiskonDokter, null)
    {
    }
}

public record TdkSaveTindakanPpaCmd(string KomponenId, string PpaId);
public record TdkSaveTindakanKomponenDiskonCmd(string KomponenId, decimal Diskon);

public record TdkSaveTindakanRespose(string TindakanId);

public class TdkSaveTindakanHandler : IRequestHandler<TdkSaveTindakanCmd, TdkSaveTindakanRespose>
{
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IBillingLockValidationService _billingLockValidator;

    public TdkSaveTindakanHandler(ITindakanRepo tindakanRepo,
        IRegRepo regRepo, ILayananRepo layananRepo, INilaiTarifRepo nilaiTarifRepo,
        IKomponenRepo komponenRepo, IPpaRepo ppaRepo,
        ITarifRepo tarifRepo, IJaminanRepo jaminanRepo,
        IAddBillAppService addBillAppService, 
        IMapJaminanJkRepo mapJaminanJkRepo, 
        IJurnalRepo jurnalRepo,
        ITglJamProvider tglJamProvider,
        IBillingLockValidationService billingLockValidator)
    {
        _tindakanRepo = tindakanRepo;
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _ppaRepo = ppaRepo;
        _tarifRepo = tarifRepo;
        _jaminanRepo = jaminanRepo;
        _addBillAppService = addBillAppService;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _jurnalRepo = jurnalRepo;
        _tglJamProvider = tglJamProvider;
        _billingLockValidator = billingLockValidator;
    }

    public TdkSaveTindakanHandler(IRegRepo regRepo,
        ILayananRepo layananRepo,
        ITarifRepo tarifRepo,
        IJaminanRepo jaminanRepo,
        INilaiTarifRepo nilaiTarifRepo,
        IKomponenRepo komponenRepo,
        IPpaRepo ppaRepo,
        ITindakanRepo tindakanRepo,
        IAddBillAppService addBillAppService,
        IMapJaminanJkRepo mapJaminanJkRepo,
        IJurnalRepo jurnalRepo,
        ITglJamProvider tglJamProvider)
        : this(tindakanRepo, regRepo, layananRepo, nilaiTarifRepo, komponenRepo, ppaRepo, tarifRepo, jaminanRepo, addBillAppService, mapJaminanJkRepo, jurnalRepo, tglJamProvider, new NullBillingLockValidationService())
    {
    }

    public async Task<TdkSaveTindakanRespose> Handle(TdkSaveTindakanCmd request, CancellationToken cancellationToken)
    {
        if (request.ListPpa is null)
            throw new ArgumentException("List PPA tidak boleh null");

        await _billingLockValidator.EnsureCanModifyBillingAsync(request.RegId, cancellationToken);

        //  BUILD
        var tindakan = LoadTindakan(request);
        var reg = LoadReg(request);
        var layanan = LoadLayanan(request);
        var nilaiTarif = LoadNilaiTaif(request);
        var tarif = LoadTarif(nilaiTarif);
        var jaminanKey = reg.TipeJaminan.TipeJaminanId[..3];
        var jaminan = LoadJaminan(JaminanType.Key(jaminanKey));

        var listKomp = ListKomponenTarif(nilaiTarif.ListKomponen.Select(x => x.Komponen)).ToList();
        var listPpa = new List<KomponenPpaView>();
        foreach (var item in request.ListPpa)
        {
            if (string.IsNullOrWhiteSpace(item.PpaId))
                continue;

            // Cari komponen yang cocok berdasarkan KomponenId
            var komp = listKomp.FirstOrDefault(x => x.KomponenId == item.KomponenId);

            // Fallback: Jika KomponenId tidak cocok persis (misal dikirim ID generik 'KOMP01' dari FE)
            // Prioritaskan komponen yang memiliki SatTugas (memerlukan PPA)
            if (komp is null)
            {
                komp = listKomp.FirstOrDefault(x => x.ListSatTugas.Any()) 
                    ?? (listKomp.Count == 1 ? listKomp[0] : null);
            }

            if (komp is null || !komp.ListSatTugas.Any())
                continue;

            var ppa = LoadPpa(PpaType.Key(item.PpaId));
            if (!komp.IsValidPpa(ppa))
                continue;

            listPpa.Add(new KomponenPpaView(komp, ppa));
        }

        var occurredAt = _tglJamProvider.Now;
        var qty = request.Qty <= 0 ? 1 : request.Qty;
        var diskonMap = request.ListKomponenDiskon?.ToDictionary(x => x.KomponenId, x => x.Diskon);
        var tdk = CreateOrEdit(tindakan, reg, layanan, nilaiTarif, listPpa, request.UserId, occurredAt, request.PakaiBedId, qty, request.IsDiskonDokter, diskonMap);

        //  WRITE
        using var trans = TransHelper.NewScope();
        var trsBilling = _addBillAppService.FromTindakan(tdk, reg, tarif, jaminan, listKomp, occurredAt);
        var mapJaminanJk = LoadMapJmnJk(jaminan);
        var jurnal = tdk == TindakanModel.Default
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(trsBilling,
                layanan, mapJaminanJk);

        _tindakanRepo.SaveChanges(tdk);
        _jurnalRepo.SaveChanges(jurnal);
        trans.Complete();

        //  RESPONSE
        return new TdkSaveTindakanRespose(tdk.TindakanId);
    }

    #region PRIVATE-HELPER
    private TindakanModel LoadTindakan(ITindakanKey key)
    {
        var result = _tindakanRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => TindakanModel.Default
            );

        return result;
    }

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
        NilaiTarifType nilaiTarif, List<KomponenPpaView> listPpa, string userId,
        DateTime occurredAt, string pakaiBedId = "", int qty = 1, bool isDiskonDokter = false,
        IReadOnlyDictionary<string, decimal>? komponenDiskonMap = null)
    {
        if (tdk.TindakanId == "-")
            return TindakanModel.Create(reg, lyn, nilaiTarif, listPpa, userId, occurredAt, pakaiBedId, qty, isDiskonDokter, komponenDiskonMap);

        tdk.AuditTrail.Modif(userId, occurredAt);

        return TindakanModel.Save(
            tdk.TindakanId, reg, lyn, nilaiTarif, listPpa, tdk.AuditTrail, occurredAt, pakaiBedId, qty, isDiskonDokter, komponenDiskonMap
        );
    }

    #endregion
}

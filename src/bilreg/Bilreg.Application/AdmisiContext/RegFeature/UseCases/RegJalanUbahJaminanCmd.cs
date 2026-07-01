using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanUbahJaminanCmd(string RegId, string TipeJaminanId, string CaraMasukDkId,
    string RujukanId, string KarcisId, string UserId) :
    IRequest, IRegKey, ITipeJaminanKey, ICaraMasukDkKey, IRujukanKey, IKarcisKey;

public class RegJalanUbahJaminanHandler : IRequestHandler<RegJalanUbahJaminanCmd>
{
    private const string BAYAR_SENDIRI = "1";

    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly IRujukanRepo _rujukanRepo;
    private readonly IKarcisRepo _karcisRepo;
    
    private readonly ILayananRepo _lynRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IJaminanRepo _jaminanRepo;

    private readonly ITarifRepo _tarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAddBillAppService _addBillAppService;
    
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;

    public RegJalanUbahJaminanHandler(IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        ICaraMasukDkRepo caraMasukDkRepo,
        IRujukanRepo rujukanRepo,
        IKarcisRepo karcisRepo,
        ILayananRepo lynRepo,
        IPasienRepo pasienRepo,
        IPolisRepo polisRepo,
        IPpaRepo ppaRepo,
        IJaminanRepo jaminanRepo,
        ITarifRepo tarifRepo,
        ITindakanRepo tindakanRepo,
        ITrsBillingRepo trsBillingRepo,
        IAddBillAppService addBillAppService,
        INilaiTarifRepo nilaiTarifRepo,
        IKomponenRepo komponenRepo,
        IJurnalRepo jurnalRepo,
        IMapJaminanJkRepo mapJaminanJkRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _rujukanRepo = rujukanRepo;
        _karcisRepo = karcisRepo;
        _lynRepo = lynRepo;
        _pasienRepo = pasienRepo;
        _polisRepo = polisRepo;
        _ppaRepo = ppaRepo;
        _jaminanRepo = jaminanRepo;
        _tarifRepo = tarifRepo;
        _tindakanRepo = tindakanRepo;
        _trsBillingRepo = trsBillingRepo;
        _addBillAppService = addBillAppService;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _jurnalRepo = jurnalRepo;
        _mapJaminanJkRepo = mapJaminanJkRepo;
    }

    public Task Handle(RegJalanUbahJaminanCmd request, CancellationToken cancellationToken)
    {
        #region GUARD & LOAD
        var regCurrent = _regAktifRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan atau sudah tidak aktif");
        var regOld = _regRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan");
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan");
        var tipeJmn = _tipeJaminanRepo.LoadEntity(request).GetValueOrThrow("Tipe jaminan invalid");
        var caraMasukDk = _caraMasukDkRepo.LoadEntity(request).GetValueOrThrow("Cara masuk Dk Invalid");
        var rujukan = _rujukanRepo.LoadEntity(request).GetValueOrDefault(RujukanType.Default); 
        var karcis = _karcisRepo.LoadEntity(request).GetValueOrThrow("Karcis not found");
        var lyn = _lynRepo.LoadEntity(LayananType.Key(reg.Layanan.LayananId)).GetValueOrThrow("Layanan Invalid");
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(reg.Pasien.PasienId)).GetValueOrThrow("Pasien invalid");
        var polis = ResolvePolis(pasien, tipeJmn);
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(reg.Dokter.PpaId)).GetValueOrThrow("Dokter not found");
        var jaminan = LoadJaminan(tipeJmn.Jaminan);
        var karcisOld = _karcisRepo.LoadEntity(KarcisType.Key(reg.Karcis.KarcisId)).GetValueOrDefault(KarcisType.Default);
        #endregion

        #region BUILD
        //  registrasi
        reg.ChangeJaminan(tipeJmn, polis, caraMasukDk, rujukan, karcis, lyn);

        //  tindakan
        var tindakan = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter, karcisOld.KarcisId);
        
        //  billing karcis
        var billKarcis = reg.Karcis.KarcisId == request.KarcisId
            ? TrsBillType.Default :
            GenBillKarcis(reg, dokter, karcis, jaminan);
        //  billing tindakan
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var billTdk = (tindakan.TindakanId != "-" && karcis.KarcisId != "-")
            ? GenBillTdk(tindakan, reg, jaminan, tarif)
            : TrsBillType.Default;

        //  jurnal-karcis
        var mapJaminanJk = LoadMapJmnJk(jaminan);
        var jurnalReg = JurnalType.CreateFromTrsBilling(billKarcis, lyn, mapJaminanJk);
        //  jurnal-tindakan
        var jurnalTindakan = tindakan.TindakanId == "-"
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(billTdk,lyn, mapJaminanJk);
        #endregion
        
        #region WRITE
        using var trans = TransHelper.NewScope();
        
        SaveRegister(reg);
        SaveBillKarcis(billKarcis);
        SaveTindakan(tindakan, reg, karcisOld);
        SaveBillTdk(billTdk);
        SaveJurnalReg(jurnalReg);
        SaveJurnalTdk(jurnalTindakan);

        trans.Complete();
        #endregion
        
        //  return

        return Task.CompletedTask;
    }

    #region PRIVATE-HELPER
    //  Load & resolve data
    private PolisModel ResolvePolis(PasienModel pasien, TipeJaminanType tipeJaminan) =>
        tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI
            ? PolisModel.Default
            : FindPolis(pasien, tipeJaminan);
    private PolisModel FindPolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        var listPolis = _polisRepo.ListData(pasien);
        var polisView = listPolis.FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff());
        return polisView == null ?
            throw new ArgumentException("Polis not found")
            : _polisRepo.LoadEntity(polisView).Value;
    }
    private JaminanType LoadJaminan(IJaminanKey key)
    {
        var jaminan = _jaminanRepo.LoadEntity(key).GetValueOrDefault(JaminanType.Default);
        return jaminan;
    }
    private TarifType LoadTarif(ITarifKey key)
    {
        return _tarifRepo.LoadEntity(key).GetValueOrDefault(TarifType.Default);
    }
    private KomponenType LoadKomponen(IKomponenKey key)
    {
        return _komponenRepo.LoadEntity(key).GetValueOrDefault(KomponenType.Default);
    }
    private MapJaminanJkType LoadMapJmnJk(IJaminanKey key)
    {
        var map = _mapJaminanJkRepo.LoadEntity(key).GetValueOrDefault(MapJaminanJkType.Default);
        return map;
    }
    
    //  Gen Data
    private TindakanModel GenTindakan(RegModel reg, JaminanType jaminan,
        KarcisType karcis, string userId, PpaType dokter, string karcisOld)
    {
        if (reg.Karcis.KarcisId != karcisOld)
        {
            var tipeTarifReff = jaminan.TipeTarif.Rajal;
            var tarifKey = karcis.DefaultTarif;
            var nilaiTarifKey = NilaiTarifType.KeyComposite(tarifKey, tipeTarifReff, reg.Kelas);
            var nilaiTarif = _nilaiTarifRepo.LoadEntity(nilaiTarifKey).GetValueOrThrow("NilaiTarif not found");

            var listKomp = _komponenRepo
                .ListData(nilaiTarif.ListKomponen.Select(x => x.Komponen))?.ToList() ?? [];
            var listPpa = listKomp
                .Where(x => x.ListSatTugas.Any())
                .Select(x => new KomponenPpaView(x, dokter));

            var tindakan = TindakanModel.FromReg(reg, nilaiTarif, listPpa, userId);
            return tindakan;
        }
        return TindakanModel.Default;
    }
    private TrsBillType GenBillKarcis(RegModel reg, PpaType dokter, KarcisType karcis, JaminanType jaminan)
    {
        var listKompKarcis = karcis.ListKomponen
            .Select(x => LoadKomponen(KomponenType.Key(x.KomponenTarif.KomponenId)))?.ToList() ?? [];
        var trsBillKarcis = _addBillAppService.FromReg(reg, karcis,
            jaminan, dokter, listKompKarcis);
        return trsBillKarcis;
    }
    private TrsBillType GenBillTdk(TindakanModel tdk, RegModel reg, JaminanType jaminan, TarifType tarif)
    {
        if (tdk.TindakanId == "-")
            return TrsBillType.Default;

        var listKomp = new List<KomponenType>();
        foreach (var item in tdk.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.Komponen.KomponenId));
            listKomp.Add(komp);
        }
        var trsBilling = _addBillAppService.FromTindakan(tdk, reg, tarif, jaminan, listKomp);
        return trsBilling;
    }

    // Save
    private void SaveRegister(RegModel reg)
    {
        _regRepo.SaveChanges(reg);
    }
    private void SaveTindakan(TindakanModel tindakan, RegModel reg, KarcisType karcisOld)
    {
        if (reg.Karcis.KarcisId != karcisOld.KarcisId)
            RemoveOldDefaultTindakan(reg, karcisOld);

        if (tindakan.TindakanId == "-")
            return;

        _tindakanRepo.SaveChanges(tindakan);
    }
    private void RemoveOldDefaultTindakan(RegModel reg, KarcisType karcisOld)
    {
        var listTdk = _tindakanRepo.ListData(reg)?.ToList() ?? [];
        var tdkDefaultOld = listTdk
            .FirstOrDefault(x => x.Tarif.TarifId == karcisOld.DefaultTarif.TarifId);

        if (tdkDefaultOld is null)
            return;

        _jurnalRepo.DeleteEntity(JurnalType.Key(tdkDefaultOld.TindakanId));
        _trsBillingRepo.DeleteEntity(TrsBillType.Key(tdkDefaultOld.TindakanId));
        _tindakanRepo.Delete(TindakanModel.Key(tdkDefaultOld.TindakanId));
    }  
    private void SaveBillTdk(TrsBillType billTdk)
    {
        if (billTdk.TrsBillingId == "-")
            return;
        _trsBillingRepo.SaveChanges(billTdk);
    }
    private void SaveBillKarcis(TrsBillType billKarcis)
    {
        if (billKarcis.TrsBillingId == "-")
            return;
        _trsBillingRepo.SaveChanges(billKarcis);
    }
    private void SaveJurnalReg(JurnalType jurnalReg)
    {
        if (jurnalReg.JurnalId == "-")
            return;
        _jurnalRepo.SaveChanges(jurnalReg);
    }
    private void SaveJurnalTdk(JurnalType jurnalTdk)
    {
        if(jurnalTdk.JurnalId == "-")
            return;
        _jurnalRepo.SaveChanges(jurnalTdk);
    }
    #endregion

}

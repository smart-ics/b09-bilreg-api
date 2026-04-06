using Ardalis.GuardClauses;
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
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegDaruratCreateCmd(string PasienId,
    string UserId,
    string TipeJaminanId,
    string CaraMasukDkId,
    string DokterId,
    string LayananId,
    string KarcisId,
    string PesertaJaminanId) : IRequest<RegDaruratCreateResponse>, 
    IPasienKey, ILayananKey, ITipeJaminanKey, ICaraMasukDkKey, IKarcisKey;

public record RegDaruratCreateResponse(string RegId);

public class RegDaruratCreateHandler : IRequestHandler<RegDaruratCreateCmd, RegDaruratCreateResponse>
{
    //  reg support
    private readonly IPasienRepo _pasienRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IKarcisRepo _karcisRepo;
    private readonly IPolisRepo _polisRepo;

    //  reg
    private readonly IRegFactory _regFactory;
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;

    //  tindakan
    private readonly IJaminanRepo _jaminanRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITarifRepo _tarifRepo;
    // trsBill
    private readonly ITrsBillingRepo _trsBillingRepo;
    // jurnal
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;

    private const string BAYAR_SENDIRI = "1";
    public RegDaruratCreateHandler(IPasienRepo pasienRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        ICaraMasukDkRepo caraMasukDkRepo,
        ILayananRepo layananRepo,
        IPpaRepo ppaRepo,
        IKarcisRepo karcisRepo,
        IPolisRepo polisRepo,

        IRegFactory regFactory,
        IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,

        IJaminanRepo jaminanRepo,
        INilaiTarifRepo nilaiTarifRepo,
        ITindakanRepo tindakanRepo,
        IKomponenRepo komponenRepo,
        ITarifRepo tarifRepo,

        ITrsBillingRepo trsBillingRepo,

        IMapJaminanJkRepo mapJaminanJkRepo,
        IJurnalRepo jurnalRepo)
    {
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _layananRepo = layananRepo;
        _ppaRepo = ppaRepo;
        _karcisRepo = karcisRepo;
        _polisRepo = polisRepo;

        _regFactory = regFactory;
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;

        _jaminanRepo = jaminanRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _tindakanRepo = tindakanRepo;
        _komponenRepo = komponenRepo;
        _tarifRepo = tarifRepo;

        _trsBillingRepo = trsBillingRepo;

        _mapJaminanJkRepo = mapJaminanJkRepo;
        _jurnalRepo = jurnalRepo;
    }

    public Task<RegDaruratCreateResponse> Handle(RegDaruratCreateCmd request, CancellationToken cancellationToken)
    {
        #region GUARD
        Guard.Against.Null(request.PesertaJaminanId, nameof(request.PesertaJaminanId));
        var pasien = _pasienRepo.LoadEntity(request).GetValueOrThrow("Pasien not found");
        if (pasien.IsAktif == false) throw new KeyNotFoundException($"Pasien {request.PasienId} tidak aktif ");
        if (_regAktifRepo.IsPasienAktif(pasien))
            throw new KeyNotFoundException($"Pasien sudah aktif registrasi");

        if (string.IsNullOrWhiteSpace(pasien.Ktp.Nik) || pasien.Ktp.Nik == "-")
            throw new KeyNotFoundException($"Nik Kosong, Lengkapi data Nik pasien {pasien.PasienId}");

        var tipeJaminan = _tipeJaminanRepo.LoadEntity(request).GetValueOrThrow("TipeJaminan not found");
        var caraMasuk = _caraMasukDkRepo.LoadEntity(request).GetValueOrThrow("CaraMasuk not found");
        var layanan = _layananRepo.LoadEntity(request).GetValueOrThrow("Layanan not found");
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId)).GetValueOrThrow("Dokter not found");
        var karcis = _karcisRepo.LoadEntity(request).GetValueOrThrow("Karcis not found");
        var polis = ResolvePolis(pasien, tipeJaminan);
        #endregion

        #region BUILD

        //      1-register
        var regMasukAudit = new AuditInfoType(request.UserId, DateTime.Now);
        var reg = _regFactory.CreateRegDarurat(pasien, regMasukAudit,
            tipeJaminan, polis, caraMasuk, dokter, layanan, karcis, request.PesertaJaminanId);
        var regAktif = RegAktifModel.CreateFromReg(reg);

        //      2-trs-billing-karcis
        var jaminan = LoadJaminan(tipeJaminan.Jaminan);
        var listKompKarcis = karcis.ListKomponen
            .Select(x => LoadKomponen(KomponenType.Key(x.KomponenTarif.KomponenId)))
            .ToList();
        var trsBillingReg = TrsBillingType.CreateFromRegistrasi(reg, karcis,
            jaminan, dokter, listKompKarcis);
        //      3-jurnal-karcis
        var mapJaminanJk = LoadMapJmnJk(tipeJaminan.Jaminan);
        var jurnalReg = JurnalType.CreateFromTrsBilling(trsBillingReg, layanan, mapJaminanJk);
        //      4-tindakan
        var tindakan = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter);
        //      5-trs-billing-tindakan
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var trsBilling = tindakan == TindakanModel.Default
            ? TrsBillingType.Default
            : GenBill(tindakan, reg, tarif, jaminan);
        //      6-jurnal-tindakan
        var jurnalTindakan = tindakan == TindakanModel.Default
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(trsBilling,
                layanan, mapJaminanJk);
        #endregion


        #region WRITE
        using (var trans = TransHelper.NewScope())
        {

            _regRepo.SaveChanges(reg);
            _regAktifRepo.SaveChanges(regAktif);
            _trsBillingRepo.SaveChanges(trsBillingReg);
            if (tindakan.TindakanId != "-")
                _tindakanRepo.SaveChanges(tindakan);
            if (trsBilling.TrsBillingId != "-")
                _trsBillingRepo.SaveChanges(trsBilling);
            _jurnalRepo.SaveChanges(jurnalReg);
            if (jurnalTindakan.JurnalId != "-")
                _jurnalRepo.SaveChanges(jurnalTindakan);
            
            trans.Complete();
            
        }

        #endregion
        var response = new RegDaruratCreateResponse(reg.RegId);
        return Task.FromResult(response);
    }

    #region PRIVATE-HELPER
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
        var jaminan = _jaminanRepo.LoadEntity(key).GetValueOrThrow($"Jaminan {key.JaminanId} not found");
        return jaminan;
    }
    private KomponenType LoadKomponen(IKomponenKey key)
    {
        var komponen = _komponenRepo.LoadEntity(key).GetValueOrThrow($"Komponen Nilai Tarif '{key.KomponenId}' invalid");
        return komponen;
    }
    private MapJaminanJkType LoadMapJmnJk(IJaminanKey key)
    {
        var map = _mapJaminanJkRepo.LoadEntity(key).GetValueOrDefault(MapJaminanJkType.Default);
        return map;
    }
    private TarifType LoadTarif(ITarifKey key)
    {
        var tarif = _tarifRepo.LoadEntity(key).GetValueOrDefault(TarifType.Default);
        return tarif;
    }


    private TindakanModel GenTindakan(RegModel reg, JaminanType jaminan,
        KarcisType karcis, string userId, PpaType dokter)
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
    private TrsBillingType GenBill(TindakanModel tdk, RegModel reg, TarifType tarif,
        JaminanType jaminan)
    {
        var listKomp = new List<KomponenType>();
        foreach (var item in tdk.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.Komponen.KomponenId));
            listKomp.Add(komp);
        }
        var trsBilling = TrsBillingType.CreateFromTindakan(tdk, reg, tarif, jaminan, listKomp);
        return trsBilling;
    }

    #endregion
}
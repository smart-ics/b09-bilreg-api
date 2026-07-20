using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanUbahKunjunganCmd(string RegId,
    string LayananId, string DokterId, string JamPraktek, string KarcisId,
    string UserId) : IRequest<RegJalanUbahKunjunganResponse>, IRegKey, ILayananKey, IKarcisKey;

public record RegJalanUbahKunjunganResponse(string RegId, int NoAntrian);
public record RegJalanUbahKunjunganHandler : IRequestHandler<RegJalanUbahKunjunganCmd, RegJalanUbahKunjunganResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;

    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IAntrianMapRepo _antrianMapRepo;
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IAntrianMapWithRegResolver _antrianMapWithRegResolver;

    private readonly ILayananRepo _layananRepo;
    private readonly IKarcisRepo _karcisRepo;

    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;

    private readonly ITindakanRepo _tindakanRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;
    private readonly ITglJamProvider _tglJamProvider;

    public RegJalanUbahKunjunganHandler(IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaRepo ppaRepo,
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        IAntrianMapRepo antrianMapRepo,
        IAntrianMapWithRegResolver antrianMapWithRegResolver,
        ILayananRepo layananRepo,
        IKarcisRepo karcisRepo,
        INilaiTarifRepo nilaiTarifRepo,
        IKomponenRepo komponenRepo,
        IJaminanRepo jaminanRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        ITarifRepo tarifRepo,
        IPasienTrackerRepo trackerRepo,
        ITrsBillingRepo trsBillingRepo,
        IAddBillAppService addBillAppService,
        ITindakanRepo tindakanRepo,
        IJurnalRepo jurnalRepo,
        IMapJaminanJkRepo mapJaminanJkRepo,
        IJadwalPraktekFeatureResolver featureResolver,
        ITglJamProvider? tglJamProvider = null)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ppaRepo = ppaRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _antrianMapRepo = antrianMapRepo;
        _antrianMapWithRegResolver = antrianMapWithRegResolver;
        _layananRepo = layananRepo;
        _karcisRepo = karcisRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _jaminanRepo = jaminanRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _tarifRepo = tarifRepo;
        _trackerRepo = trackerRepo;
        _trsBillingRepo = trsBillingRepo;
        _addBillAppService = addBillAppService;
        _tindakanRepo = tindakanRepo;
        _jurnalRepo = jurnalRepo;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _featureResolver = featureResolver;
        _tglJamProvider = tglJamProvider;
    }
    public Task<RegJalanUbahKunjunganResponse> Handle(RegJalanUbahKunjunganCmd request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        #region GUARD & LOAD
        var regCurrent = _regAktifRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan atau sudah tidak aktif");
        var regOld = _regRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan");
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan");
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId)).GetValueOrThrow("Dokter not found");
        var layanan = _layananRepo.LoadEntity(request).GetValueOrThrow("Layanan not found");
        var karcis = _karcisRepo.LoadEntity(request).GetValueOrThrow("Karcis not found");
        var tipeJaminan = LoadTipeJaminan(TipeJaminanType.Key(reg.TipeJaminan.TipeJaminanId));
        var jaminan = LoadJaminan(tipeJaminan.Jaminan);
        var karcisOld = _karcisRepo.LoadEntity(KarcisType.Key(reg.Karcis.KarcisId)).GetValueOrDefault(KarcisType.Default);
        #endregion

        #region BUILD
        //  antrian
        var tglBerobat = reg.RegDate;
        var schedule = BookingScheduleResolver.ResolveWalkIn(
            _featureResolver, _jadwalPraktekRepo, dokter, tglBerobat, request.JamPraktek);
        var antrian = ResolveAntrian(tglBerobat, dokter, schedule);
        var antrianMap = _antrianMapWithRegResolver.Resolve(schedule.LegacyJadwal, tglBerobat, reg);
        var tracker = PasienTrackerModel.Create(reg, occurredAt);

        //  registrasi
        reg.ChangeDataKunjungan(layanan, karcis, dokter);
        var regAktif = RegAktifModel.CreateFromReg(reg);

        //  tindakan
        var tindakan = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter, request.KarcisId, occurredAt);

        //  billing karcis
        var billKarcis = reg.Karcis.KarcisId == request.KarcisId
            ? TrsBillType.Default :
            GenBillKarcis(reg, dokter, karcis, jaminan, occurredAt);
        //  billing tindakan
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var billTdk = (tindakan.TindakanId != "-" && karcis.KarcisId != "-")
            ? GenBillTdk(tindakan, reg, jaminan, tarif, occurredAt)
            : TrsBillType.Default;

        //  jurnal-karcis
        var mapJaminanJk = LoadMapJmnJk(jaminan);
        var jurnalReg = JurnalType.CreateFromTrsBilling(billKarcis, layanan, mapJaminanJk);
        //  jurnal-tindakan
        var jurnalTindakan = tindakan.TindakanId == "-"
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(billTdk, layanan, mapJaminanJk);
        #endregion

        #region WRITER
        using var trans = TransHelper.NewScope();

        SaveRegister(reg, regAktif);
        SaveAntrian(antrian, antrianMap.Value.Item1, tracker, antrianMap.Value.Item2.NoUrut, reg, regOld, occurredAt);
        SaveBillKarcis(billKarcis);
        SaveTindakan(tindakan, reg, karcisOld);
        SaveBillTdk(billTdk);
        SaveJurnalReg(jurnalReg);
        SaveJurnalTdk(jurnalTindakan);

        trans.Complete();
        #endregion

        var result = new RegJalanUbahKunjunganResponse(reg.RegId, antrianMap.Value.Item2.NoUrut);
        return Task.FromResult(result);
    }

    #region PRIVATE-HELPER
    //  Load & Resolve
    private AntrianModel ResolveAntrian(DateOnly tgl, PpaType dokter, BookingScheduleContext schedule)
    {
        var listAntrian = _antrianRepo.ListData(tgl);
        var tag = _featureResolver.UseResolver
            ? AntrianModel.GenSequenceTag(tgl, schedule.Effective)
            : AntrianModel.GenSequenceTag(tgl, schedule.LegacyJadwal.JamMulai, dokter);
        var existingView = listAntrian.FirstOrDefault(x => x.SequenceTag == tag);
        return existingView is null
            ? (_featureResolver.UseResolver
                ? _antrianFactory.Create(tgl, schedule.Effective)
                : _antrianFactory.Create(tgl, schedule.LegacyJadwal))
            : _antrianRepo.LoadEntity(existingView).Value;
    }
    private TipeJaminanType LoadTipeJaminan(ITipeJaminanKey key)
    {
        return _tipeJaminanRepo.LoadEntity(key).GetValueOrDefault(TipeJaminanType.Default);
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
    
    //  Gen Data
    private TindakanModel GenTindakan(RegModel reg, JaminanType jaminan,
        KarcisType karcis, string userId, PpaType dokter, string karcisReq, DateTime occurredAt)
    {
        if (reg.Karcis.KarcisId != karcisReq)
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

            var tindakan = TindakanModel.FromReg(reg, nilaiTarif, listPpa, userId, occurredAt);
            return tindakan;
        }
        return TindakanModel.Default;
    }
    private TrsBillType GenBillKarcis(RegModel reg, PpaType dokter, KarcisType karcis, JaminanType jaminan, DateTime occurredAt)
    {
        var listKompKarcis = karcis.ListKomponen
            .Select(x => LoadKomponen(KomponenType.Key(x.KomponenTarif.KomponenId)))?.ToList() ?? [];
        var trsBillKarcis = _addBillAppService.FromReg(reg, karcis,
            jaminan, dokter, listKompKarcis, occurredAt);
        return trsBillKarcis;

    }
    private TrsBillType GenBillTdk(TindakanModel tdk, RegModel reg, JaminanType jaminan, TarifType tarif, DateTime occurredAt)
    {
        if (tdk.TindakanId == "-")
            return TrsBillType.Default;

        var listKomp = new List<KomponenType>();
        foreach (var item in tdk.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.Komponen.KomponenId));
            listKomp.Add(komp);
        }
        var trsBilling = _addBillAppService.FromTindakan(tdk, reg, tarif, jaminan, listKomp, occurredAt);
        return trsBilling;
    }
    private KomponenType LoadKomponen(IKomponenKey key)
    {
        return _komponenRepo.LoadEntity(key).GetValueOrDefault(KomponenType.Default);
    }
    private AntrianMapModel LoadAntrianMap(RegModel reg, AntrianModel que)
    {
        var ppaKey = PpaType.Key(reg.Dokter.PpaId);
        var lynKey = LayananType.Key(reg.Layanan.LayananId);
        var listAntrianMap = _antrianMapRepo
            .ListData(lynKey, ppaKey, reg.RegDate)?
            .ToList() ?? [];

        var antrianThis = listAntrianMap.FirstOrDefault(x => x.JamJadwal == que.StartTime);
        var result = antrianThis is null ?
            AntrianMapModel.Default :
            _antrianMapRepo.LoadEntity(AntrianMapModel.Key(antrianThis.AntrianMapId)).Value;

        return result;
    }
    private (AntrianModel Que, int NoUrut) LoadAntrianOldContext(RegModel reg)
    {
        var queDate = reg.RegDate.ToDateTime(TimeOnly.MinValue);
        var listQue = _antrianRepo.ListData(queDate)?.ToList() ?? [];

        var queReg = listQue.FirstOrDefault(x => x.ReffId == reg.RegId);
        if (queReg is null)
            return (AntrianModel.Default, 0);

        var que = _antrianRepo
            .LoadEntity(AntrianModel.Key(queReg.AntrianId))
            .GetValueOrDefault(AntrianModel.Default);

        return (
            que, queReg.NoUrut
        );
    }
    private MapJaminanJkType LoadMapJmnJk(IJaminanKey key)
    {
        var map = _mapJaminanJkRepo.LoadEntity(key).GetValueOrDefault(MapJaminanJkType.Default);
        return map;
    }

    //  Save
    private void SaveAntrian(AntrianModel antrian, AntrianMapModel antrianMapNew,
        PasienTrackerModel tracker, int noAntrian, RegModel regNew, RegModel regOld, DateTime occurredAt)
    {
        //      delete antrian lama
        var queOldContext = LoadAntrianOldContext(regOld);
        var antrianMapVoid = LoadAntrianMap(regOld, queOldContext.Que);
        VoidAntrianMap(antrianMapVoid, queOldContext.NoUrut);
        VoidAntrian(queOldContext.Que, queOldContext.NoUrut, tracker);

        var antEntry = antrian.AddEntry(noAntrian, tracker, regNew.RegId, "REG", occurredAt);
        var itemQueue = antrian.ListEntry.FirstOrDefault(x => x.NoUrut == noAntrian)
            ?? AntrianEntryModel.Default;
        itemQueue.Serve(occurredAt);
        _antrianRepo.SaveChanges(antrian);
        _trackerRepo.SaveChanges(tracker);
        //      antrianMap
        _antrianMapRepo.SaveChanges(antrianMapNew);


    }
    private void SaveRegister(RegModel reg, RegAktifModel regAktif)
    {
        _regRepo.SaveChanges(reg);
        _regAktifRepo.Delete(reg);
        _regAktifRepo.SaveChanges(regAktif);
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
    private void VoidAntrianMap(AntrianMapModel queMap, int noUrut)
    {
        if (queMap.AntrianMapId == "-")
            return;

        queMap.VoidSlot(noUrut);
        _antrianMapRepo.SaveChanges(queMap);
    }
    private void VoidAntrian(AntrianModel Que, int NoUrut, IPasienTrackerKey TrackerKey)
    {
        if (Que.AntrianId == "-")
            return;

        Que.RemoveEntry(NoUrut);
        _antrianRepo.SaveChanges(Que);
        _trackerRepo.DeleteEntity(TrackerKey);
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
        if (jurnalTdk.JurnalId == "-")
            return;
        _jurnalRepo.SaveChanges(jurnalTdk);
    }
    #endregion
}


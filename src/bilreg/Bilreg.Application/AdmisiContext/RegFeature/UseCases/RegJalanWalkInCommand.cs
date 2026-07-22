using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RemoteCetakFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RemotCetakFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;
using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanWalkInCommand(string PasienId, string UserId,
    string TipeJaminanId, string CaraMasukDkId, string RujukanId, string DokterId,
    string LayananId, string JamPraktek, string KarcisId, string PesertaJaminanId,
    string? AdmissionAntrianId = null,
    int? AdmissionNoUrut = null,
    string? AdmissionServicePointCode = null,
    string? AdmissionServicePointName = null) 
    : IRequest<RegJalanCreateResponse>, ILayananKey, ICaraMasukDkKey, IPasienKey,
        ITipeJaminanKey, IKarcisKey; 
 
public record RegJalanCreateResponse(string RegId, int NoAntrian);

public class RegJalanCreateHandler : IRequestHandler<RegJalanWalkInCommand, RegJalanCreateResponse>
{
    //  reg support
    private readonly IPasienRepo _pasienRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly IRujukanRepo _rujukanRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IKarcisRepo _karcisRepo;
    //  reg
    private readonly IRegFactory _regFactory;
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    //  antrian
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IAntrianMapRepo _antrianMapRepo;
    //  tindakan
    private readonly IJaminanRepo _jaminanRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITarifRepo _tarifRepo;
    // trsBill
    private readonly ITrsBillingRepo _trsBillingRepo;

    private readonly IAddBillAppService _addBillAppService;
    // jurnal
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private const string BAYAR_SENDIRI = "1";

    private readonly IRemoteCetakRepo _remoteCetakRepo;
    private readonly IGetAppSettingService _getAppSettingSvc;
    private readonly EmrAntrianOutboundEnqueueService _emrOutboundEnqueue;
    private readonly IQueueNumberCompatibilityAdapter _queueNumberAdapter;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IAdmissionServicePointResolver _admissionServicePointResolver;

    public RegJalanCreateHandler(
        //  reg support
        IPasienRepo pasienRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo,
        ICaraMasukDkRepo caraMasukDkRepo,
        IRujukanRepo rujukanRepo,
        ILayananRepo layananRepo,
        IPpaRepo ppaRepo,
        IKarcisRepo karcisRepo,
        //  registrasi
        IRegFactory regFactory,
        IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        //  antrian
        IPasienTrackerRepo trackerRepo,
        IAntrianFactory antrianFactory,
        IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianMapRepo antrianMapRepo,
        //  tindakan
        IJaminanRepo jaminanRepo,
        INilaiTarifRepo nilaiTarifRepo,
        ITindakanRepo tindakanRepo,
        IKomponenRepo komponenRepo,
        ITarifRepo tarifRepo,
        //  trsBill
        ITrsBillingRepo trsBillingRepo,
        IAddBillAppService addBillAppService,
        // jurnal
        IMapJaminanJkRepo mapJaminanJkRepo,
        IJurnalRepo jurnalRepo,
        IRemoteCetakRepo remoteCetakRepo,
        IGetAppSettingService getAppSettingSvc,
        EmrAntrianOutboundEnqueueService emrOutboundEnqueue,
        IQueueNumberCompatibilityAdapter queueNumberAdapter,
        IJadwalPraktekFeatureResolver featureResolver,
        ITglJamProvider tglJamProvider,
        IAdmissionServicePointResolver admissionServicePointResolver)
    {
        //      reg-support
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _rujukanRepo = rujukanRepo;
        _layananRepo = layananRepo;
        _ppaRepo = ppaRepo;
        _karcisRepo = karcisRepo;
        //      reg
        _regFactory = regFactory;
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        //      antrian
        _trackerRepo = trackerRepo;
        _antrianFactory = antrianFactory;
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianMapRepo = antrianMapRepo;
        //      tindakan
        _jaminanRepo = jaminanRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _tindakanRepo = tindakanRepo;
        _komponenRepo = komponenRepo;
        _tarifRepo = tarifRepo;
        //      trsBill
        _trsBillingRepo = trsBillingRepo;
        _addBillAppService = addBillAppService;
        //      jurnal
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _jurnalRepo = jurnalRepo;
        _remoteCetakRepo = remoteCetakRepo;
        _getAppSettingSvc = getAppSettingSvc;
        _emrOutboundEnqueue = emrOutboundEnqueue;
        _queueNumberAdapter = queueNumberAdapter;
        _featureResolver = featureResolver;
        _tglJamProvider = tglJamProvider;
        _admissionServicePointResolver = admissionServicePointResolver;
    }

    public Task<RegJalanCreateResponse> Handle(RegJalanWalkInCommand request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        /*  ▐▀▀▀▀▀▀▀▀▀▀▀▌
            ▐   GUARD   ▌
            ▐▄▄▄▄▄▄▄▄▄▄▄▌*/
        Guard.Against.Null(request.PesertaJaminanId, nameof(request.PesertaJaminanId));
        var pasien = _pasienRepo.LoadEntity(request).GetValueOrThrow("Pasien not found");
        if (pasien.IsAktif == false) throw new KeyNotFoundException($"Pasien {request.PasienId} tidak aktif ");
        if(IsPasienAktifReg(pasien))
            throw new KeyNotFoundException($"Pasien sudah aktif registrasi");

        if (IsAdult(pasien.Person.TglLahir, DateOnly.FromDateTime(occurredAt)) && (string.IsNullOrWhiteSpace(pasien.Ktp.Nik) || pasien.Ktp.Nik == "-"))
            throw new KeyNotFoundException($"Nik Kosong, Lengkapi data Nik pasien {pasien.PasienId}");
        
        var tipeJaminan = _tipeJaminanRepo.LoadEntity(request).GetValueOrThrow("TipeJaminan not found");
        var polis = ResolvePolis(pasien, tipeJaminan);
        var caraMasuk = _caraMasukDkRepo.LoadEntity(request).GetValueOrThrow("CaraMasuk not found");
        var rujukan = ResolveRujukan(caraMasuk, request.RujukanId);
        var layanan = _layananRepo.LoadEntity(request).GetValueOrThrow("Layanan not found");
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId)).GetValueOrThrow("Dokter not found");
        var karcis = _karcisRepo.LoadEntity(request).GetValueOrThrow("Karcis not found");
        /*  ▐▀▀▀▀▀▀▀▀▀▀▀▌
            ▐   BUILD   ▌
            ▐▄▄▄▄▄▄▄▄▄▄▄▌*/
        //      1-register
        var regMasukAudit = new AuditInfoType(request.UserId, occurredAt);
        var reg = _regFactory.CreateRegRajal(pasien, regMasukAudit,
            tipeJaminan, polis, caraMasuk, rujukan, dokter, layanan, karcis, request.PesertaJaminanId);
        var regAktif = RegAktifModel.CreateFromReg(reg);
        //      2-antrian
        var tglBerobat = DateOnly.FromDateTime(occurredAt);
        var schedule = BookingScheduleResolver.ResolveWalkIn(
            _featureResolver, _jadwalPraktekRepo, dokter, tglBerobat, request.JamPraktek);
        var antrian = ResolveAntrian(tglBerobat, dokter, schedule);
        var reserved = _queueNumberAdapter.ReserveForRegistration(
            schedule.LegacyJadwal, tglBerobat, reg).Value;

        //      3-trs-billing-karcis
        var jaminan = LoadJaminan(tipeJaminan.Jaminan);
        var listKompKarcis = karcis.ListKomponen
            .Select(x => LoadKomponen(KomponenType.Key(x.KomponenTarif.KomponenId)))
            .ToList();
        var trsBillingReg = _addBillAppService.FromReg(reg, karcis,
            jaminan, dokter, listKompKarcis, occurredAt);
        //      4-jurnal-karcis
        var mapJaminanJk = LoadMapJmnJk(tipeJaminan.Jaminan);
        var jurnalReg = JurnalType.CreateFromTrsBilling(trsBillingReg, layanan, mapJaminanJk);
        //      5-tindakan
        var tindakan =  karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter, occurredAt);
        //      6-trs-billing-tindakan
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var trsBilling = tindakan == TindakanModel.Default 
            ? TrsBillType.Default
            : GenBill(tindakan, reg, tarif, jaminan, occurredAt);
        //      7-jurnal-tindakan
        var jurnalTindakan = tindakan == TindakanModel.Default
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(trsBilling,
                layanan, mapJaminanJk);
        //      8-remote Cetak
        var appSetting = _getAppSettingSvc.Execute();
        var rmtCetak = new RemoteCetakType(
            reg.RegId, "RG-ANTRIAN", occurredAt,
            appSetting.Registrasi.RemoteCetakRegistrasi,
            false, new DateTime(3000, 1, 1),
            "", "");
        /*  ▐▀▀▀▀▀▀▀▀▀▀▀▌
            ▐   WRITE   ▌
            ▐▄▄▄▄▄▄▄▄▄▄▄▌*/

        RegJalanCreateResponse response;
        
        using (var trans = TransHelper.NewScope())
        {
            PasienTrackerModel tracker;
            AntrianModel admissionQueue;
            if (!string.IsNullOrWhiteSpace(request.AdmissionAntrianId)
                && request.AdmissionNoUrut is > 0)
            {
                admissionQueue = _antrianRepo.LoadEntity(
                        AntrianModel.Key(request.AdmissionAntrianId!))
                    .GetValueOrThrow($"Admission queue '{request.AdmissionAntrianId}' not found");
                _admissionServicePointResolver.EnsureAdmissionQueue(admissionQueue);
                var resolution = AdmissionQueueRegistrationResolver.ResolveAndComplete(
                    _trackerRepo, admissionQueue, request.AdmissionNoUrut.Value, reg, occurredAt);
                tracker = resolution.Tracker;
                if (resolution.RequiresConditionalSave
                    && !_antrianRepo.TrySaveAnonymousInServiceTransition(
                        admissionQueue, resolution.Entry))
                {
                    throw new AdmissionQueueConcurrencyException(
                        $"Queue entry '{admissionQueue.AntrianId}' / {resolution.Entry.NoUrut} was changed concurrently.");
                }
            }
            else
            {
                tracker = PasienTrackerModel.Create(reg, occurredAt);
                admissionQueue = AdmissionQueueComplete.CompleteAtRegistration(
                    _antrianRepo, _antrianFactory, tracker, reg.RegId, occurredAt,
                    null, null,
                    _admissionServicePointResolver.ServicePoint,
                    _admissionServicePointResolver);
            }

            // Physician entry stays Waiting until MulaiPeriksa; legacy "active" = ReffDesc REG / AntrianMap (F-07).
            var antEntry = _queueNumberAdapter.ProjectIntoQueueSession(
                antrian, reserved, tracker, reg.RegId, "REG", occurredAt);
            _regRepo.SaveChanges(reg);
            _regAktifRepo.SaveChanges(regAktif);
            _antrianRepo.SaveChanges(antrian);
            _antrianRepo.SaveChanges(admissionQueue);
            _trackerRepo.SaveChanges(tracker);
            _antrianMapRepo.SaveChanges(reserved.Map);
            _trsBillingRepo.SaveChanges(trsBillingReg);
            if (tindakan.TindakanId != "-")
                _tindakanRepo.SaveChanges(tindakan);
            if (trsBilling.TrsBillingId != "-")
                _trsBillingRepo.SaveChanges(trsBilling);
            _jurnalRepo.SaveChanges(jurnalReg);
            if (jurnalTindakan.JurnalId != "-")
                _jurnalRepo.SaveChanges(jurnalTindakan);
            _remoteCetakRepo.SaveChanges(rmtCetak);

            var emrPayload = new AddAntrianEmrByRegCommand(
                reg.RegId, "-", reg.Pasien.PasienId,
                reg.Pasien.PasienName, reg.Layanan.LayananId,
                reg.Dokter.PpaId, reg.RegDate.ToString("yyyy-MM-dd"),
                schedule.LegacyJadwal.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
                antEntry.NoUrut);
            _emrOutboundEnqueue.TryEnqueueAddReg(emrPayload, occurredAt);

            trans.Complete();
            response = new RegJalanCreateResponse(reg.RegId, antEntry.NoUrut);
        }

        return Task.FromResult(response);
        
    }

    #region PRIVATE-HELPERS
    private bool IsPasienAktifReg(IPasienKey pasien)
    {
        return _regAktifRepo.IsPasienAktif(pasien)
            || _regRepo.IsPasienAktifReg(pasien);
    }
    public bool IsAdult(DateOnly TglLahir, DateOnly today)
    {
        int age = today.Year - TglLahir.Year;
        // Koreksi jika ulang tahun belum lewat tahun ini
        if (today < TglLahir.AddYears(age))
            age--;
        
        return age > 17;
    }
    private PolisModel ResolvePolis(PasienModel pasien, TipeJaminanType tipeJaminan) =>
        tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI
            ? PolisModel.Default
            : FindPolis(pasien, tipeJaminan);

    private RujukanType ResolveRujukan(CaraMasukDkType caraMasuk, string rujukanId) =>
        !caraMasuk.RequiresRujukan
            ? RujukanType.Default
            : _rujukanRepo.LoadEntity(RujukanType.Key(rujukanId))
                .GetValueOrThrow("'Rujukan' not found");

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

    private PolisModel FindPolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        var listPolis = _polisRepo.ListData(pasien);
        var polisView = listPolis.FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff());
        return polisView == null ? 
            throw new ArgumentException("Polis not found") 
            : _polisRepo.LoadEntity(polisView).Value;
    }

    private TindakanModel GenTindakan(RegModel reg, JaminanType jaminan, 
        KarcisType karcis, string userId, PpaType dokter, DateTime occurredAt)
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

    private TrsBillType GenBill(TindakanModel tdk, RegModel reg, TarifType tarif,
        JaminanType jaminan, DateTime occurredAt)
    {
        var listKomp = new List<KomponenType>();
        foreach(var item in tdk.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.Komponen.KomponenId));
            listKomp.Add(komp);
        }
        var trsBilling = _addBillAppService.FromTindakan(tdk, reg, tarif, jaminan, listKomp, occurredAt);
        return trsBilling;
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
    private JaminanType LoadJaminan(IJaminanKey key)
    {
        var jaminan = _jaminanRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Jaminan {key.JaminanId} not found")
            );
        return jaminan;
    }
    private TarifType LoadTarif(ITarifKey key)
    {
        var tarif = _tarifRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => TarifType.Default
            );
        return tarif;
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
    #endregion
}

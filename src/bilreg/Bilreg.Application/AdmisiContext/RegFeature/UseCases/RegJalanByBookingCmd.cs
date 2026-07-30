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
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Application.Shared.Helpers;
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
using Ardalis.GuardClauses;
using System.Globalization;
using System.Text.Json.Serialization;
using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanByBookingCmd(
    string BookingId, string UserId, string KarcisId,
    string CaraMasukDkId, string RujukanId, string TipeJaminanId, string PesertaJaminanId,
    string? AdmissionAntrianId = null,
    int? AdmissionNoUrut = null,
    string? AdmissionExpectedRowVersion = null) : IRequest<RegJalanByBookingResponse>
{
    [JsonIgnore]
    public string? AdmissionLoketKey { get; init; }

    [JsonIgnore]
    public RegistrationAdmissionQueueBehavior AdmissionQueueBehavior { get; init; }
}

public record RegJalanByBookingResponse(string RegId, int NoAntrian);
public class RegJalanByBookingHandler 
    : IRequestHandler<RegJalanByBookingCmd, RegJalanByBookingResponse>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IRegFactory _regFactory;
    private readonly IPpaRepo _dokterRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IKarcisRepo _karcisRepo;
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly IRujukanRepo _rujukanRepo;
    
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITipeTarifRepo _tipeTarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAddBillAppService _addBillAppService;
    
    private readonly IRemoteCetakRepo _remoteCetakRepo;
    private readonly IGetAppSettingService _getAppSettingSvc;

    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly EmrAntrianOutboundEnqueueService _emrOutboundEnqueue;
    private readonly IAntrianMapRepo _antrianMapRepo;
    private readonly IQueueNumberCompatibilityAdapter _queueNumberAdapter;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IAdmissionServicePointResolver _admissionServicePointResolver;
    private readonly IRegistrationOutcomeOperationRepo? _registrationOutcomeRepo;
    private readonly IAdmissionQueueRefreshPublisher? _admissionQueueRefreshPublisher;

    private const string BAYAR_SENDIRI = "1";
    public RegJalanByBookingHandler(
        IBookingRepo bookingRepo,
        IPasienRepo pasienRepo,
        IRegFactory regFactory,
        IPpaRepo dokterRepo,
        ILayananRepo layananRepo,
        IKarcisRepo karcisRepo,
        IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        ICaraMasukDkRepo caraMasukDkRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo,
        IRujukanRepo rujukanRepo,
        IJaminanRepo jaminanRepo,
        ITarifRepo tarifRepo,
        INilaiTarifRepo nilaiTarifRepo,
        ITipeTarifRepo tipeTarifRepo,
        ITindakanRepo tindakanRepo,
        IKomponenRepo komponenRepo,
        ITrsBillingRepo trsBillingRepo,
        IAddBillAppService addBillAppService,
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        IPasienTrackerRepo trackerRepo,
        IMapJaminanJkRepo mapJaminanJkRepo,
        IJurnalRepo jurnalRepo,
        IRemoteCetakRepo remoteCetakRepo,
        IGetAppSettingService getAppSettingSvc,
        EmrAntrianOutboundEnqueueService emrOutboundEnqueue,
        IAntrianMapRepo antrianMapRepo,
        IQueueNumberCompatibilityAdapter queueNumberAdapter,
        ITglJamProvider tglJamProvider,
        IAdmissionServicePointResolver admissionServicePointResolver,
        IRegistrationOutcomeOperationRepo? registrationOutcomeRepo = null,
        IAdmissionQueueRefreshPublisher? admissionQueueRefreshPublisher = null)
    {
        _bookingRepo = bookingRepo;
        _pasienRepo = pasienRepo;
        _regFactory = regFactory;
        _dokterRepo = dokterRepo;
        _layananRepo = layananRepo;
        _karcisRepo = karcisRepo;
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _rujukanRepo = rujukanRepo;
        _jaminanRepo = jaminanRepo;
        _tarifRepo = tarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _tipeTarifRepo = tipeTarifRepo;
        _tindakanRepo = tindakanRepo;
        _komponenRepo = komponenRepo;
        _trsBillingRepo = trsBillingRepo;
        _addBillAppService = addBillAppService;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _trackerRepo = trackerRepo;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _jurnalRepo = jurnalRepo;
        _remoteCetakRepo = remoteCetakRepo;
        _getAppSettingSvc = getAppSettingSvc;
        _emrOutboundEnqueue = emrOutboundEnqueue;
        _antrianMapRepo = antrianMapRepo;
        _queueNumberAdapter = queueNumberAdapter;
        _tglJamProvider = tglJamProvider;
        _admissionServicePointResolver = admissionServicePointResolver;
        _registrationOutcomeRepo = registrationOutcomeRepo;
        _admissionQueueRefreshPublisher = admissionQueueRefreshPublisher;
    }

    public async Task<RegJalanByBookingResponse> Handle(RegJalanByBookingCmd request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        var admissionContext = request.AdmissionQueueBehavior == RegistrationAdmissionQueueBehavior.QueueLinked
            ? AdmissionRegistrationQueueContextResolver.Resolve(
                request.AdmissionAntrianId,
                request.AdmissionNoUrut,
                request.AdmissionExpectedRowVersion,
                request.AdmissionLoketKey)
            : null;
        if (request.AdmissionQueueBehavior == RegistrationAdmissionQueueBehavior.None &&
            AdmissionRegistrationQueueContextResolver.HasAnyDirectQueueField(
                request.AdmissionAntrianId,
                request.AdmissionNoUrut,
                request.AdmissionExpectedRowVersion))
            throw new ArgumentException("Direct Registration must not include Admission Queue context.");
        if (admissionContext is not null)
            EnsureAdmissionEntryInService(admissionContext);
        //  LOAD and GUARD
        Guard.Against.Null(request.PesertaJaminanId, nameof(request.PesertaJaminanId));
        var booking = LoadBooking(request.BookingId);
        if (!string.IsNullOrWhiteSpace(booking.Reg.RegId) && booking.Reg.RegId != "-")
            throw new InvalidOperationException(
                $"Booking already registered as {booking.Reg.RegId}.");
        var antrian = LoadAntrian(booking, DateOnly.FromDateTime(occurredAt));
        var pasien = LoadPasien(booking.PasienId);
        if (IsPasienAktifReg(pasien))
            throw new KeyNotFoundException($"Pasien sudah aktif registrasi");
        if (IsAdult(pasien.Person.TglLahir, DateOnly.FromDateTime(occurredAt)) && (string.IsNullOrWhiteSpace(pasien.Ktp.Nik) || pasien.Ktp.Nik == "-"))
            throw new KeyNotFoundException($"Nik Kosong, Lengkapi data Nik pasien {pasien.PasienId}");

        var dokter = LoadDokter(booking.Dokter.PpaId);
        var layanan = LoadLayanan(booking.Layanan.LayananId); 
        var karcis = LoadKarcis(request.KarcisId);
        var caraMasuk = LoadCaraMasuk(request.CaraMasukDkId);
        var tipeJaminan = LoadTipeJaminan(request.TipeJaminanId);
        var polis = ResolvePolis(pasien, tipeJaminan);
        var rujukan = ResolveRujukan(caraMasuk, request.RujukanId);

        //  BUILD
        var regAudit = new AuditInfoType(request.UserId, occurredAt);
        var reg = _regFactory.CreateRegRajal(
            pasien, regAudit, tipeJaminan,
            polis, caraMasuk, rujukan,
            dokter, layanan, karcis, request.PesertaJaminanId);
        booking.AssignReg(reg);

        var regAktif = new RegAktifModel(reg.RegId,  reg.RegDate, 
            reg.Pasien, reg.JenisReg, reg.Layanan,
            reg.Dokter, reg.TipeJaminan);

        //  ANTRIAN (physician): retag to REG for legacy projection; stay Waiting until MulaiPeriksa (F-07).
        var itemQueue = antrian.ListEntry.FirstOrDefault(x => x.NoUrut == booking.NoAntrian) 
            ?? AntrianEntryModel.Default;
        itemQueue.SetReff(reg.RegId, "REG");

        //      BUILD TINDAKAN
        var jaminan = LoadJaminan(tipeJaminan.Jaminan);
        var listKompKarcis = new List<KomponenType>();
        foreach (var item in karcis.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.KomponenTarif.KomponenId));
            listKompKarcis.Add(komp);
        }
        var trsBillingReg = _addBillAppService.FromReg(reg, karcis,
            jaminan, dokter, listKompKarcis, occurredAt);

        var tindakan = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter, occurredAt);


        //     BUILD Jurnal Reg
        var mapJaminanJk = LoadMapJmnJk(tipeJaminan.Jaminan);
        var jurnalReg = JurnalType.CreateFromTrsBilling(trsBillingReg, 
            layanan, mapJaminanJk);

        
        //      BUILD TrsBill
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var trsBilling = tindakan == TindakanModel.Default
            ? TrsBillType.Default
            : GenBill(tindakan, reg, tarif, jaminan, occurredAt);

        //      BUILD Jurnal Tindakan
        var jurnalTindakan = tindakan == TindakanModel.Default
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(trsBilling,
                layanan, mapJaminanJk);
        //      REMOTE-CETAK
        var appSetting = _getAppSettingSvc.Execute();
        var rmtCetak = new RemoteCetakType(
            reg.RegId, "RG-ANTRIAN", occurredAt,
            appSetting.Registrasi.RemoteCetakRegistrasi, 
            false, new DateTime(3000, 1, 1),
            "", "");


        //  WRITE
        RegJalanByBookingResponse response;
        using (var trans = TransHelper.NewScope())
        {
            var tracker = _trackerRepo.LoadEntity(itemQueue.Tracker)
                .GetValueOrThrow($"PasienTracker '{itemQueue.Tracker.PasienTrackerId}' not found");
            AntrianModel? admissionQueue = null;
            if (admissionContext is not null)
            {
                admissionQueue = _antrianRepo.LoadEntity(AntrianModel.Key(admissionContext.AntrianId))
                    .GetValueOrThrow($"Admission queue '{admissionContext.AntrianId}' not found");
                _admissionServicePointResolver.EnsureAdmissionQueue(admissionQueue);
                var admissionEntry = AdmissionQueueComplete.RequireIdentifiedInServiceEntry(
                    admissionQueue, admissionContext.NoUrut, tracker);
                AdmissionQueueComplete.CompleteInServiceEntry(
                    admissionEntry, tracker, reg.RegId, occurredAt);
            }
            else
                AdmissionQueueComplete.AppendRegisterIfMissing(tracker, reg.RegId, occurredAt);

            var physicianMap = PhysicianAntrianMapLookup.FindForBooking(_antrianMapRepo, booking);
            if (_queueNumberAdapter.ProjectSourceReffForRegistration(physicianMap, booking.NoAntrian, reg))
                _antrianMapRepo.SaveChanges(physicianMap);

            _regRepo.SaveChanges(reg);
            _bookingRepo.SaveChanges(booking);
            _regAktifRepo.SaveChanges(regAktif);
            _trsBillingRepo.SaveChanges(trsBillingReg);
            _antrianRepo.SaveChanges(antrian);
            if (admissionContext is not null)
            {
                var admissionEntry = admissionQueue!.ListEntry
                    .First(x => x.NoUrut == admissionContext.NoUrut);
                var outcome = RegistrationOutcomeModel.Established(admissionQueue.AntrianId,
                    admissionEntry.NoUrut, reg.RegId, request.UserId, occurredAt);
                if (_registrationOutcomeRepo is null)
                    throw new InvalidOperationException(
                        "Registration outcome persistence is required for queue-linked Registration.");
                var finalized = _registrationOutcomeRepo.TryFinalizeEstablished(
                    outcome,
                    admissionEntry,
                    admissionContext.LoketKey,
                    admissionContext.ExpectedRowVersion,
                    occurredAt);
                if (!finalized)
                {
                    throw new AdmissionQueueConcurrencyException(
                        $"Queue entry '{admissionQueue.AntrianId}' / {admissionEntry.NoUrut} was changed concurrently.");
                }
            }
            _trackerRepo.SaveChanges(tracker);
            if (tindakan.TindakanId != "-")
                _tindakanRepo.SaveChanges(tindakan);
            if (trsBilling.TrsBillingId != "-")
                _trsBillingRepo.SaveChanges(trsBilling);

            _jurnalRepo.SaveChanges(jurnalReg);
            if (jurnalTindakan.JurnalId != "-")
                _jurnalRepo.SaveChanges(jurnalTindakan);

            _remoteCetakRepo.SaveChanges(rmtCetak);

            var emrPayload = new AddAntrianEmrByRegCommand(
                reg.RegId, booking.BookingId, reg.Pasien.PasienId,
                reg.Pasien.PasienName, reg.Layanan.LayananId,
                reg.Dokter.PpaId, reg.RegDate.ToString("yyyy-MM-dd"),
                booking.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture),
                booking.NoAntrian);
            _emrOutboundEnqueue.TryEnqueueAddReg(emrPayload, occurredAt);

            trans.Complete();
            response = new RegJalanByBookingResponse(reg.RegId, booking.NoAntrian);
        }

        if (admissionContext is not null && _admissionQueueRefreshPublisher is not null)
            await _admissionQueueRefreshPublisher.PublishAsync(admissionContext.LoketKey, cancellationToken);

        return response;
    }

    #region PRIVATE HELPER
    private void EnsureAdmissionEntryInService(AdmissionRegistrationQueueContext context)
    {
        var queue = _antrianRepo.LoadEntity(AntrianModel.Key(context.AntrianId))
            .GetValueOrThrow($"Admission queue '{context.AntrianId}' not found");
        var entry = queue.ListEntry.FirstOrDefault(x => x.NoUrut == context.NoUrut)
            ?? throw new KeyNotFoundException(
                $"Queue entry '{context.AntrianId}' / {context.NoUrut} not found");
        if (entry.AntrianStatus != AntrianStatusEnum.InService)
            throw new AdmissionQueueConcurrencyException(
                $"Queue entry '{context.AntrianId}' / {context.NoUrut} is no longer In Service.");
    }

    public bool IsAdult(DateOnly TglLahir, DateOnly today)
    {
        int age = today.Year - TglLahir.Year;

        // Koreksi jika ulang tahun belum lewat tahun ini
        if (today < TglLahir.AddYears(age))
            age--;

        return age > 17;
    }
    private BookingModel LoadBooking(string id)
    {
        var booking = _bookingRepo.LoadEntity(BookingModel.Key(id))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {id} tidak ditemukan"));
        if (booking.HasBeenRegistered())
            throw new ArgumentException($"Booking {id} sudah teregister di {booking.Reg.RegId}");
        return booking;
    }

    private PasienModel LoadPasien(string id) =>
        _pasienRepo.LoadEntity(PasienModel.Key(id))
            .GetValueOrThrow("Pasien tidak ditemukan");
    private bool IsPasienAktifReg(IPasienKey pasien)
    {
        return _regAktifRepo.IsPasienAktif(pasien)
            || _regRepo.IsPasienAktifReg(pasien);
    }
    private PpaType LoadDokter(string id) =>
        _dokterRepo.LoadEntity(PpaType.Key(id))
            .GetValueOrThrow("Dokter tidak valid");

    private LayananType LoadLayanan(string id) =>
        _layananRepo.LoadEntity(LayananType.Key(id))
            .GetValueOrThrow("Layanan tidak valid");

    private KarcisType LoadKarcis(string id) =>
        _karcisRepo.LoadEntity(KarcisType.Key(id))
            .GetValueOrThrow("Karcis tidak valid");

    private CaraMasukDkType LoadCaraMasuk(string id) =>
        _caraMasukDkRepo.LoadEntity(CaraMasukDkType.Key(id))
            .GetValueOrThrow("'Cara Masuk' not found");

    private TipeJaminanType LoadTipeJaminan(string id) =>
        _tipeJaminanRepo.LoadEntity(TipeJaminanType.Key(id))
            .GetValueOrThrow("Tipe Jaminan invalid");

    private PolisModel ResolvePolis(PasienModel pasien, TipeJaminanType tipeJaminan) =>
        tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI
            ? PolisModel.Default
            : FindPolis(pasien, tipeJaminan);
    private PolisModel FindPolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        var listPolis = _polisRepo.ListData(pasien);
        var polisView = listPolis.FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff());
        if (polisView == null)
            throw new ArgumentException("Polis not found");
        return _polisRepo.LoadEntity(polisView).Value;
    }
    private RujukanType ResolveRujukan(CaraMasukDkType caraMasuk, string rujukanId) =>
        !caraMasuk.RequiresRujukan
            ? RujukanType.Default
            : _rujukanRepo.LoadEntity(RujukanType.Key(rujukanId))
                .GetValueOrThrow("'Rujukan' not found");

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
    
    private AntrianModel LoadAntrian(BookingModel booking, DateOnly date)
    {
        var ppa = LoadDokter(booking.Dokter.PpaId);
        var listQueue = _antrianRepo.ListData(date)?.ToList() ?? [];
        var squesceTag = AntrianModel.GenSequenceTag(date, booking.JamPraktek, ppa);
        var antrian = listQueue.FirstOrDefault(x => x.SequenceTag == squesceTag) 
            ?? new AntrianHeaderView("-", "-", DateOnly.MinValue, TimeOnly.MinValue, "");

        var result = _antrianRepo.LoadEntity(antrian).GetValueOrThrow("Antrian not found");
        return result;
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

using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmissionRegistrationData(
    string TipeJaminanId,
    string CaraMasukDkId,
    string ProsedurMasukInapId,
    string? RujukanId,
    string DokterId,
    string PesertaJaminanId,
    string? JenisInapId = null,
    string? RegIbuId = null) :
    ITipeJaminanKey, ICaraMasukDkKey, IProsedurMasukInapKey;

public interface IAdmissionRegistrationOrchestrator
{
    Task<AdmProcessAdmissionResponse> ProcessOpnameRequest(
        AdmProcessOpnameRequestCmd request, CancellationToken cancellationToken);

    Task<AdmProcessAdmissionResponse> ProcessReservation(
        AdmProcessReservationCmd request, CancellationToken cancellationToken);
}

public class AdmissionRegistrationOrchestrator : IAdmissionRegistrationOrchestrator
{
    private const string BAYAR_SENDIRI = "1";

    private readonly IAdmissionRepo _admissionRepo;
    private readonly IOpnameRequestRepo _opnameRequestRepo;
    private readonly IReservationRepo _reservationRepo;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IBangsalRepo _bangsalRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly ICaraMasukDkRepo _caraMasukRepo;
    private readonly IRujukanRepo _rujukanRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IProsedureMasukInapRepo _prosedurMasukInapRepo;
    private readonly IRegFactory _regFactory;
    private readonly IRegRepo _regRepo;
    private readonly IRegInapRepo _regInapRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IRegDal _regDal;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IAdmisiEventPublisher _publisher;
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IPasienTrackerInpatientCoordinator _trackerCoordinator;
    private readonly ILogger<AdmissionRegistrationOrchestrator> _logger;

    public AdmissionRegistrationOrchestrator(
        IAdmissionRepo admissionRepo,
        IOpnameRequestRepo opnameRequestRepo,
        IReservationRepo reservationRepo,
        IWardAccommodationGateway wardGateway,
        IBangsalRepo bangsalRepo,
        IPasienRepo pasienRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo,
        ICaraMasukDkRepo caraMasukRepo,
        IRujukanRepo rujukanRepo,
        IPpaRepo ppaRepo,
        ILayananRepo layananRepo,
        IProsedureMasukInapRepo prosedurMasukInapRepo,
        IRegFactory regFactory,
        IRegRepo regRepo,
        IRegInapRepo regInapRepo,
        IRegAktifRepo regAktifRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider,
        IAdmisiEventPublisher publisher,
        IWaitingListRepo waitingListRepo,
        IPasienTrackerInpatientCoordinator trackerCoordinator,
        IRegDal? regDal = null,
        ILogger<AdmissionRegistrationOrchestrator>? logger = null)
    {
        _admissionRepo = admissionRepo;
        _opnameRequestRepo = opnameRequestRepo;
        _reservationRepo = reservationRepo;
        _wardGateway = wardGateway;
        _bangsalRepo = bangsalRepo;
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _caraMasukRepo = caraMasukRepo;
        _rujukanRepo = rujukanRepo;
        _ppaRepo = ppaRepo;
        _layananRepo = layananRepo;
        _prosedurMasukInapRepo = prosedurMasukInapRepo;
        _regFactory = regFactory;
        _regRepo = regRepo;
        _regInapRepo = regInapRepo;
        _regAktifRepo = regAktifRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _publisher = publisher;
        _waitingListRepo = waitingListRepo;
        _trackerCoordinator = trackerCoordinator ?? throw new ArgumentNullException(nameof(trackerCoordinator));
        _regDal = regDal!;
        _logger = logger ?? NullLogger<AdmissionRegistrationOrchestrator>.Instance;
    }

    public async Task<AdmProcessAdmissionResponse> ProcessOpnameRequest(
        AdmProcessOpnameRequestCmd request, CancellationToken cancellationToken)
    {
        ValidateRequest(request.OpnameRequestId, request.KelasDkId, request.BangsalId,
            request.UserId, request.Registration);
        var occurredAt = _tglJamProvider.Now;
        var opname = _opnameRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Opname Request '{request.OpnameRequestId}' tidak ditemukan.");
        if (opname.OpnameRequestStatus != OpnameRequestStatusEnum.Requested)
            throw new InvalidOperationException(
                $"Opname Request '{opname.OpnameRequestId}' harus Requested untuk diproses (status: {opname.OpnameRequestStatus}).");

        EnsurePatientAvailable(opname.Pasien);
        var prosedur = ResolveProsedurMasukInap(request.Registration);
        var admission = CreateAdmission(opname.Pasien, request.KelasDkId, request.BangsalId,
            opname.OpnameRequestId, null, request.UserId, occurredAt);
        var reg = CreateRegistration(admission, request.Registration);
        var regInap = CreateRegInap(admission, reg, prosedur);
        var fulfilled = opname.Fulfill(admission.RegId, request.UserId, occurredAt);
        var snapshot = AuditLogSnapshotJson.Serialize(opname);

        using (var trans = TransHelper.NewScope())
        {
            SaveCommon(admission, reg, regInap);
            _opnameRequestRepo.SaveChanges(fulfilled);
            SaveSourceAudit(fulfilled.AuditTrail.Modified, nameof(OpnameRequestModel),
                fulfilled.OpnameRequestId, snapshot);
            trans.Complete();
        }

        RecordTrackerRegistrationGracefully(opname, admission.RegId, occurredAt);

        await _publisher.PublishRanapCreatedAsync(admission.RegId, cancellationToken);

        var waitingListId = PlaceWaitingListGracefully(
            admission, request.KelasId, request.BangsalId, request.KelasDkId,
            request.Priority, request.UserId, occurredAt);

        return ToResponse(admission, waitingListId);
    }

    public async Task<AdmProcessAdmissionResponse> ProcessReservation(
        AdmProcessReservationCmd request, CancellationToken cancellationToken)
    {
        ValidateRequest(request.ReservationId, request.KelasDkId, request.BangsalId,
            request.UserId, request.Registration);
        var occurredAt = _tglJamProvider.Now;
        var reservation = _reservationRepo.LoadEntity(request)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");

        EnsurePatientAvailable(reservation.Pasien);
        var prosedur = ResolveProsedurMasukInap(request.Registration);
        var bangsal = _wardGateway.ResolveBangsalForCareClass(request.BangsalId, request.KelasDkId);
        if (reservation.ReservationStatus == ReservationStatusEnum.Reserved)
            reservation = reservation.Maintain(
                reservation.PlannedDate, reservation.KelasRawat, bangsal, request.UserId, occurredAt);

        if (reservation.ReservationStatus != ReservationStatusEnum.Maintained)
            throw new InvalidOperationException(
                $"Reservation '{reservation.ReservationId}' harus Maintained untuk direalisasi (status: {reservation.ReservationStatus}).");

        var admission = CreateAdmission(reservation.Pasien, request.KelasDkId, request.BangsalId,
            null, reservation.ReservationId, request.UserId, occurredAt, bangsal);
        var reg = CreateRegistration(admission, request.Registration);
        var regInap = CreateRegInap(admission, reg, prosedur);
        var realized = reservation.Realize(admission.RegId, request.UserId, occurredAt);
        var snapshot = AuditLogSnapshotJson.Serialize(reservation);

        using (var trans = TransHelper.NewScope())
        {
            SaveCommon(admission, reg, regInap);
            _reservationRepo.SaveChanges(realized);
            SaveSourceAudit(realized.AuditTrail.Modified, nameof(ReservationModel),
                realized.ReservationId, snapshot);
            trans.Complete();
        }

        RecordTrackerRegistrationGracefully(reservation, admission.RegId, occurredAt);

        await _publisher.PublishRanapCreatedAsync(admission.RegId, cancellationToken);

        var waitingListId = PlaceWaitingListGracefully(
            admission, request.KelasId, request.BangsalId, request.KelasDkId,
            request.Priority, request.UserId, occurredAt, reservation.KelasRawat);

        return ToResponse(admission, waitingListId);
    }

    private string? PlaceWaitingListGracefully(
        AdmissionModel admission,
        string? kelasId,
        string bangsalId,
        string kelasDkId,
        int priority,
        string userId,
        DateTime occurredAt,
        KelasReff? fallbackKelas = null)
    {
        try
        {
            var kelasRawat = ResolveKelasRawat(kelasId, bangsalId, kelasDkId, fallbackKelas);
            var bangsal = admission.Bangsal ?? _wardGateway.ResolveBangsal(bangsalId);

            var waitingList = WaitingListModel.Create(
                admission.RegId,
                admission.AdmissionStatus,
                admission.Pasien,
                kelasRawat,
                bangsal,
                priority,
                userId,
                occurredAt);

            _waitingListRepo.SaveChanges(waitingList);

            _auditRepo.SaveChanges(AuditLog.Create(
                waitingList.AuditTrail.Created,
                "CREATE",
                nameof(WaitingListModel),
                waitingList.WaitingListId));

            _wardGateway.NotifyHandOver(new WardAccommodationHandOver(
                waitingList.WaitingListId,
                waitingList.RegId,
                waitingList.Bangsal.BangsalId,
                waitingList.KelasRawat.KelasId,
                waitingList.WaitingListStatus));

            return waitingList.WaitingListId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Registrasi ranap {RegId} berhasil dibuat, namun otomasi waiting list mengalami kendala: {ErrorMessage}",
                admission.RegId, ex.Message);
            return null;
        }
    }

    private void RecordTrackerRegistrationGracefully(
        OpnameRequestModel opname,
        string regId,
        DateTime occurredAt)
    {
        try
        {
            _trackerCoordinator.RecordAdmissionRegistered(opname, regId, occurredAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Registrasi ranap {RegId} berhasil dibuat, namun pencatatan tracker pasien dari OpnameRequest {OpnameRequestId} mengalami kendala: {ErrorMessage}",
                regId, opname.OpnameRequestId, ex.Message);
        }
    }

    private void RecordTrackerRegistrationGracefully(
        ReservationModel reservation,
        string regId,
        DateTime occurredAt)
    {
        try
        {
            _trackerCoordinator.RecordAdmissionRegistered(reservation, regId, occurredAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Registrasi ranap {RegId} berhasil dibuat, namun pencatatan tracker pasien dari Reservation {ReservationId} mengalami kendala: {ErrorMessage}",
                regId, reservation.ReservationId, ex.Message);
        }
    }

    private KelasReff ResolveKelasRawat(
        string? kelasId,
        string bangsalId,
        string kelasDkId,
        KelasReff? fallbackKelas = null)
    {
        if (!string.IsNullOrWhiteSpace(kelasId))
            return _wardGateway.ResolveKelas(kelasId);
        if (fallbackKelas is not null && !string.IsNullOrWhiteSpace(fallbackKelas.KelasId))
            return fallbackKelas;
        return _wardGateway.ResolveKelasForCareClass(bangsalId, kelasDkId);
    }

    private AdmissionModel CreateAdmission(PasienReff pasien, string kelasDkId, string bangsalId,
        string? opnameRequestId, string? reservationId, string userId, DateTime occurredAt,
        BangsalReff? resolvedBangsal = null)
    {
        var kelasDk = _wardGateway.ResolveKelasDk(kelasDkId);
        var bangsal = resolvedBangsal ?? _wardGateway.ResolveBangsalForCareClass(bangsalId, kelasDkId);
        return AdmissionModel.Admit(pasien, kelasDk, bangsal, opnameRequestId, reservationId, userId, occurredAt);
    }

    private RegModel CreateRegistration(AdmissionModel admission, AdmissionRegistrationData data)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(admission.Pasien.PasienId))
            .GetValueOrThrow($"Pasien '{admission.Pasien.PasienId}' tidak ditemukan.");
        var tipeJaminan = _tipeJaminanRepo.LoadEntity(data)
            .GetValueOrThrow($"Tipe Jaminan '{data.TipeJaminanId}' tidak ditemukan.");
        var caraMasuk = _caraMasukRepo.LoadEntity(data)
            .GetValueOrThrow($"Cara Masuk '{data.CaraMasukDkId}' tidak ditemukan.");
        var rujukan = ResolveRujukan(caraMasuk, data.RujukanId);
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(data.DokterId))
            .GetValueOrThrow($"Dokter '{data.DokterId}' tidak ditemukan.");
        var layanan = ResolveLayananFromBangsal(admission.Bangsal.BangsalId);
        var polis = ResolvePolis(pasien, tipeJaminan);

        var (jenisInap, regIbu) = ResolveJenisInapAndRegIbu(pasien, admission, data);

        return _regFactory.CreateRegInapFromAdmission(admission, pasien, tipeJaminan, polis,
            caraMasuk, rujukan, dokter, layanan, data.PesertaJaminanId, jenisInap, regIbu);
    }


    private (JenisInapType JenisInap, string RegIbu) ResolveJenisInapAndRegIbu(
        PasienModel pasien,
        AdmissionModel admission,
        AdmissionRegistrationData data)
    {
        var regDate = DateOnly.FromDateTime(admission.AuditTrail.Created.Timestamp);
        var isPerinatal = InpatientAgePolicy.IsPerinatalAge(pasien.Person.TglLahir, regDate);

        if (isPerinatal)
        {
            var regIbuId = data.RegIbuId;
            if (string.IsNullOrWhiteSpace(regIbuId))
            {
                var mrIbu = pasien.MrIbu;
                if (!string.IsNullOrWhiteSpace(mrIbu))
                {
                    var regsIbu = _regDal?.ListData(PasienModel.Key(mrIbu)) ?? [];
                    var activeMotherReg = regsIbu
                        .Where(r => r.fd_tgl_void == "3000-01-01" && r.fd_tgl_keluar == "3000-01-01")
                        .OrderByDescending(r => r.fd_tgl_masuk)
                        .ThenByDescending(r => r.fs_jam_masuk)
                        .FirstOrDefault();

                    if (activeMotherReg is not null)
                    {
                        regIbuId = activeMotherReg.fs_kd_reg;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(regIbuId))
                throw new InvalidOperationException("Pasien perinatal wajib menyertakan Register Ibu yang valid.");

            var motherReg = _regDal?.GetData(RegModel.Key(regIbuId))
                ?? throw new InvalidOperationException($"Register Ibu '{regIbuId}' tidak ditemukan.");

            if (motherReg.fd_tgl_void != "3000-01-01")
                throw new InvalidOperationException($"Register Ibu '{regIbuId}' sudah dibatalkan (void).");

            if (motherReg.fd_tgl_keluar != "3000-01-01")
                throw new InvalidOperationException($"Register Ibu '{regIbuId}' sudah keluar/pulang.");

            if (motherReg.fs_kd_jenis_inap != InpatientAgePolicy.JENIS_INAP_OBSTETRI)
                throw new InvalidOperationException("JENIS INAP IBU invalid");

            return (JenisInapType.Perinatal, regIbuId);
        }

        var jenisInapId = InpatientAgePolicy.ResolveJenisInap(
            pasien.Person.TglLahir, regDate, data.JenisInapId);
        var jenisInap = JenisInapType.FromId(jenisInapId);
        return (jenisInap, string.Empty);
    }

    private LayananType ResolveLayananFromBangsal(string bangsalId)
    {
        var bangsal = _bangsalRepo.LoadEntity(BangsalType.Key(bangsalId))
            .GetValueOrThrow($"Bangsal '{bangsalId}' tidak ditemukan.");

        var mappedLayananId = bangsal.Layanan.LayananId;
        if (string.IsNullOrWhiteSpace(mappedLayananId) || mappedLayananId == "-")
            throw new InvalidOperationException(
                $"Bangsal '{bangsalId}' tidak memiliki mapping Layanan Rawat Inap.");

        var layanan = _layananRepo.LoadEntity(LayananType.Key(mappedLayananId))
            .GetValueOrThrow(
                $"Layanan '{mappedLayananId}' untuk Bangsal '{bangsalId}' tidak ditemukan.");

        if (layanan.InstalasiDk.InstalasiDkId != InstalasiDkType.RawatInap.InstalasiDkId)
            throw new InvalidOperationException(
                $"Layanan '{layanan.LayananId}' untuk Bangsal '{bangsalId}' bukan instalasi rawat inap.");

        return layanan;
    }

    private RujukanType ResolveRujukan(CaraMasukDkType caraMasuk, string? rujukanId)
    {
        if (!caraMasuk.RequiresRujukan)
            return RujukanType.Default;

        Guard.Against.NullOrWhiteSpace(rujukanId, nameof(AdmissionRegistrationData.RujukanId));
        return _rujukanRepo.LoadEntity(RujukanType.Key(rujukanId))
            .GetValueOrThrow($"Rujukan '{rujukanId}' tidak ditemukan.");
    }

    private static RegInapModel CreateRegInap(
        AdmissionModel admission,
        RegModel reg,
        ProsedurMasukInapType prosedur)
        => RegInapModel.Create(
            reg.RegId,
            prosedur,
            reg.Dokter,
            DateOnly.FromDateTime(admission.AuditTrail.Created.Timestamp));

    private PolisModel ResolvePolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        if (tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI)
            return PolisModel.Default;

        var polisView = _polisRepo.ListData(pasien)
            .FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff())
            ?? throw new ArgumentException("Polis not found");
        return _polisRepo.LoadEntity(polisView).GetValueOrThrow("Polis not found");
    }

    private ProsedurMasukInapType ResolveProsedurMasukInap(AdmissionRegistrationData data)
        => _prosedurMasukInapRepo.LoadEntity(data)
            .GetValueOrThrow(
                $"Prosedur Masuk Inap '{data.ProsedurMasukInapId}' tidak ditemukan.");

    private void EnsurePatientAvailable(PasienReff pasien)
    {
        var activeAdmission = _admissionRepo
            .ListData(new AdmissionListFilter(PasienId: pasien.PasienId))
            .FirstOrDefault(x => x.AdmissionStatus is not AdmissionStatusEnum.Completed
                and not AdmissionStatusEnum.Cancelled);
        if (activeAdmission is not null)
            throw new InvalidOperationException(
                $"Pasien '{pasien.PasienId}' masih memiliki admission aktif ({activeAdmission.RegId}).");

        if (_regAktifRepo.IsPasienAktif(PasienModel.Key(pasien.PasienId)))
            throw new InvalidOperationException(
                $"Pasien '{pasien.PasienId}' masih memiliki registrasi aktif.");
    }

    private void SaveCommon(AdmissionModel admission, RegModel reg, RegInapModel regInap)
    {
        _admissionRepo.SaveChanges(admission);
        _regRepo.SaveChanges(reg);
        _regInapRepo.SaveChanges(regInap);
        _regAktifRepo.SaveChanges(RegAktifModel.CreateFromReg(reg));
        _auditRepo.SaveChanges(AuditLog.Create(
            admission.AuditTrail.Created, "CREATE", nameof(AdmissionModel), admission.RegId));
    }

    private void SaveSourceAudit(AuditInfoType audit, string entityName, string entityId, string snapshot)
        => _auditRepo.SaveChanges(AuditLog.Create(
            audit, "UPDATE", entityName, entityId, originalDataJson: snapshot));

    private static AdmProcessAdmissionResponse ToResponse(AdmissionModel admission, string? waitingListId = null)
        => new(admission.RegId, admission.AdmissionStatus, waitingListId);

    private static void ValidateRequest(string sourceId, string kelasDkId, string bangsalId,
        string userId, AdmissionRegistrationData data)
    {
        Guard.Against.NullOrWhiteSpace(sourceId);
        Guard.Against.NullOrWhiteSpace(kelasDkId);
        Guard.Against.NullOrWhiteSpace(bangsalId);
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.Null(data);
        Guard.Against.NullOrWhiteSpace(data.TipeJaminanId);
        Guard.Against.NullOrWhiteSpace(data.CaraMasukDkId);
        Guard.Against.NullOrWhiteSpace(data.ProsedurMasukInapId);
        Guard.Against.NullOrWhiteSpace(data.DokterId);
        Guard.Against.Null(data.PesertaJaminanId);
    }
}

using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
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
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmissionRegistrationData(
    string TipeJaminanId,
    string CaraMasukDkId,
    string ProsedurMasukInapId,
    string? RujukanId,
    string DokterId,
    string PesertaJaminanId) :
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
    private readonly IAuditRepo _auditRepo;

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
        IAuditRepo auditRepo)
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
    }

    public Task<AdmProcessAdmissionResponse> ProcessOpnameRequest(
        AdmProcessOpnameRequestCmd request, CancellationToken cancellationToken)
    {
        ValidateRequest(request.OpnameRequestId, request.KelasDkId, request.BangsalId,
            request.UserId, request.Registration);
        var opname = _opnameRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Opname Request '{request.OpnameRequestId}' tidak ditemukan.");
        if (opname.OpnameRequestStatus != OpnameRequestStatusEnum.Requested)
            throw new InvalidOperationException(
                $"Opname Request '{opname.OpnameRequestId}' harus Requested untuk diproses (status: {opname.OpnameRequestStatus}).");

        EnsurePatientAvailable(opname.Pasien);
        var prosedur = ResolveProsedurMasukInap(request.Registration);
        var admission = CreateAdmission(opname.Pasien, request.KelasDkId, request.BangsalId,
            opname.OpnameRequestId, null, request.UserId);
        var reg = CreateRegistration(admission, request.Registration);
        var regInap = CreateRegInap(admission, reg, prosedur);
        var fulfilled = opname.Fulfill(admission.RegId, request.UserId);
        var snapshot = AuditLogSnapshotJson.Serialize(opname);

        using var trans = TransHelper.NewScope();
        SaveCommon(admission, reg, regInap);
        _opnameRequestRepo.SaveChanges(fulfilled);
        SaveSourceAudit(fulfilled.AuditTrail.Modified, nameof(OpnameRequestModel),
            fulfilled.OpnameRequestId, snapshot);
        trans.Complete();

        return Task.FromResult(ToResponse(admission));
    }

    public Task<AdmProcessAdmissionResponse> ProcessReservation(
        AdmProcessReservationCmd request, CancellationToken cancellationToken)
    {
        ValidateRequest(request.ReservationId, request.KelasDkId, request.BangsalId,
            request.UserId, request.Registration);
        var reservation = _reservationRepo.LoadEntity(request)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");

        EnsurePatientAvailable(reservation.Pasien);
        var prosedur = ResolveProsedurMasukInap(request.Registration);
        var bangsal = _wardGateway.ResolveBangsalForCareClass(request.BangsalId, request.KelasDkId);
        if (reservation.ReservationStatus == ReservationStatusEnum.Reserved)
            reservation = reservation.Maintain(
                reservation.PlannedDate, reservation.KelasRawat, bangsal, request.UserId);

        if (reservation.ReservationStatus != ReservationStatusEnum.Maintained)
            throw new InvalidOperationException(
                $"Reservation '{reservation.ReservationId}' harus Maintained untuk direalisasi (status: {reservation.ReservationStatus}).");

        var admission = CreateAdmission(reservation.Pasien, request.KelasDkId, request.BangsalId,
            null, reservation.ReservationId, request.UserId, bangsal);
        var reg = CreateRegistration(admission, request.Registration);
        var regInap = CreateRegInap(admission, reg, prosedur);
        var realized = reservation.Realize(admission.RegId, request.UserId);
        var snapshot = AuditLogSnapshotJson.Serialize(reservation);

        using var trans = TransHelper.NewScope();
        SaveCommon(admission, reg, regInap);
        _reservationRepo.SaveChanges(realized);
        SaveSourceAudit(realized.AuditTrail.Modified, nameof(ReservationModel),
            realized.ReservationId, snapshot);
        trans.Complete();

        return Task.FromResult(ToResponse(admission));
    }

    private AdmissionModel CreateAdmission(PasienReff pasien, string kelasDkId, string bangsalId,
        string? opnameRequestId, string? reservationId, string userId, BangsalReff? resolvedBangsal = null)
    {
        var kelasDk = _wardGateway.ResolveKelasDk(kelasDkId);
        var bangsal = resolvedBangsal ?? _wardGateway.ResolveBangsalForCareClass(bangsalId, kelasDkId);
        return AdmissionModel.Admit(pasien, kelasDk, bangsal, opnameRequestId, reservationId, userId);
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

        return _regFactory.CreateRegInapFromAdmission(admission, pasien, tipeJaminan, polis,
            caraMasuk, rujukan, dokter, layanan, data.PesertaJaminanId);
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

    private static AdmProcessAdmissionResponse ToResponse(AdmissionModel admission)
        => new(admission.RegId, admission.AdmissionStatus);

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

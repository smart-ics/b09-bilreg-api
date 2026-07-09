using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessReservationCmd(
    string ReservationId,
    string KelasDkId,
    string BangsalId,
    string UserId) : IRequest<AdmProcessAdmissionResponse>, IReservationKey;

public class AdmProcessReservationHandler : IRequestHandler<AdmProcessReservationCmd, AdmProcessAdmissionResponse>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IReservationRepo _reservationRepo;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;

    public AdmProcessReservationHandler(
        IAdmissionRepo admissionRepo,
        IReservationRepo reservationRepo,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo)
    {
        _admissionRepo = admissionRepo;
        _reservationRepo = reservationRepo;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
    }

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessReservationCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReservationId);
        Guard.Against.NullOrWhiteSpace(request.KelasDkId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var reservation = _reservationRepo.LoadEntity(request)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");
        var pasien = reservation.Pasien;

        var activeAdmissions = _admissionRepo
            .ListData(new AdmissionListFilter(PasienId: pasien.PasienId))
            .Where(a => a.AdmissionStatus is not AdmissionStatusEnum.Completed
                and not AdmissionStatusEnum.Cancelled)
            .ToList();

        if (activeAdmissions.Count > 0)
            throw new InvalidOperationException(
                $"Pasien '{pasien.PasienId}' masih memiliki admission aktif ({activeAdmissions[0].RegId}).");

        var kelasDk = _wardGateway.ResolveKelasDk(request.KelasDkId);
        var bangsal = _wardGateway.ResolveBangsalForCareClass(request.BangsalId, request.KelasDkId);

        if (reservation.ReservationStatus == ReservationStatusEnum.Reserved)
        {
            reservation = reservation.Maintain(
                reservation.PlannedDate,
                reservation.KelasRawat,
                bangsal,
                request.UserId);
        }

        if (reservation.ReservationStatus != ReservationStatusEnum.Maintained)
            throw new InvalidOperationException(
                $"Reservation '{reservation.ReservationId}' harus Maintained untuk direalisasi (status: {reservation.ReservationStatus}).");

        var reservationSnapshot = AuditLogSnapshotJson.Serialize(reservation);

        var admission = AdmissionModel.Admit(
            pasien,
            kelasDk,
            bangsal,
            null,
            reservation.ReservationId,
            request.UserId);

        var realized = reservation.Realize(admission.RegId, request.UserId);

        using var trans = TransHelper.NewScope();
        _admissionRepo.SaveChanges(admission);
        _reservationRepo.SaveChanges(realized);

        _auditRepo.SaveChanges(AuditLog.Create(
            admission.AuditTrail.Created,
            "CREATE",
            nameof(AdmissionModel),
            admission.RegId));

        _auditRepo.SaveChanges(AuditLog.Create(
            realized.AuditTrail.Modified,
            "UPDATE",
            nameof(ReservationModel),
            realized.ReservationId,
            originalDataJson: reservationSnapshot));

        trans.Complete();

        return Task.FromResult(new AdmProcessAdmissionResponse(
            admission.RegId,
            admission.AdmissionStatus));
    }
}

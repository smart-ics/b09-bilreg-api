using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessReservationCmd(
    string ReservationId,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest<AdmProcessAdmissionResponse>, IReservationKey;

public class AdmProcessReservationHandler : IRequestHandler<AdmProcessReservationCmd, AdmProcessAdmissionResponse>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IReservationRepo _reservationRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public AdmProcessReservationHandler(
        IAdmissionRepo admissionRepo,
        IReservationRepo reservationRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo)
    {
        _admissionRepo = admissionRepo;
        _reservationRepo = reservationRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessReservationCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReservationId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var reservation = AdmisiRanapSupport.LoadReservation(_reservationRepo, request);
        var pasien = reservation.Pasien;

        AdmisiRanapSupport.EnsureNoActiveAdmission(_admissionRepo, pasien.PasienId);

        var kelas = AdmisiRanapSupport.LoadKelasReff(_kelasRepo, request.KelasId);
        var bangsal = AdmisiRanapSupport.LoadBangsalReff(_bangsalRepo, request.BangsalId);

        if (reservation.ReservationStatus == ReservationStatusEnum.Reserved)
        {
            reservation = reservation.Maintain(
                reservation.PlannedDate,
                kelas,
                bangsal,
                request.UserId);
        }

        if (reservation.ReservationStatus != ReservationStatusEnum.Maintained)
            throw new InvalidOperationException(
                $"Reservation '{reservation.ReservationId}' harus Maintained untuk direalisasi (status: {reservation.ReservationStatus}).");

        var admission = AdmissionModel.Admit(
            pasien,
            kelas,
            bangsal,
            null,
            reservation.ReservationId,
            request.UserId);

        var realized = reservation.Realize(admission.RegId, request.UserId);

        using var trans = TransHelper.NewScope();
        _admissionRepo.SaveChanges(admission);
        _reservationRepo.SaveChanges(realized);
        trans.Complete();

        return Task.FromResult(new AdmProcessAdmissionResponse(
            admission.RegId,
            admission.AdmissionStatus));
    }
}

using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;

public record AdmMaintainReservationCmd(
    string ReservationId,
    DateTime PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest, IReservationKey;

public class AdmMaintainReservationHandler : IRequestHandler<AdmMaintainReservationCmd>
{
    private readonly IReservationRepo _reservationRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public AdmMaintainReservationHandler(
        IReservationRepo reservationRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo)
    {
        _reservationRepo = reservationRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task Handle(AdmMaintainReservationCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReservationId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelas = AdmisiRanapSupport.LoadKelasReff(_kelasRepo, request.KelasId);
        var bangsal = AdmisiRanapSupport.LoadBangsalReff(_bangsalRepo, request.BangsalId);

        var reservation = AdmisiRanapSupport.LoadReservation(_reservationRepo, request);
        var maintained = reservation.Maintain(request.PlannedDate, kelas, bangsal, request.UserId);

        _reservationRepo.SaveChanges(maintained);
        return Task.CompletedTask;
    }
}

using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

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
    private readonly IWardAccommodationGateway _wardGateway;

    public AdmMaintainReservationHandler(
        IReservationRepo reservationRepo,
        IWardAccommodationGateway wardGateway)
    {
        _reservationRepo = reservationRepo;
        _wardGateway = wardGateway;
    }

    public Task Handle(AdmMaintainReservationCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReservationId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);

        var reservation = _reservationRepo.LoadEntity(request)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");
        var maintained = reservation.Maintain(request.PlannedDate, kelas, bangsal, request.UserId);

        _reservationRepo.SaveChanges(maintained);
        return Task.CompletedTask;
    }
}

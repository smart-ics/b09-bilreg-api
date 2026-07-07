using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.ReservationFeature.UseCases;

public record AdmCreateReservationCmd(
    string PasienId,
    DateTime PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest<AdmCreateReservationResponse>;

public record AdmCreateReservationResponse(string ReservationId);

public class AdmCreateReservationHandler : IRequestHandler<AdmCreateReservationCmd, AdmCreateReservationResponse>
{
    private readonly IReservationRepo _reservationRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public AdmCreateReservationHandler(
        IReservationRepo reservationRepo,
        IPasienRepo pasienRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo)
    {
        _reservationRepo = reservationRepo;
        _pasienRepo = pasienRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task<AdmCreateReservationResponse> Handle(
        AdmCreateReservationCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var pasien = AdmisiRanapSupport.LoadPasienReff(_pasienRepo, request.PasienId);
        var kelas = AdmisiRanapSupport.LoadKelasReff(_kelasRepo, request.KelasId);
        var bangsal = AdmisiRanapSupport.LoadBangsalReff(_bangsalRepo, request.BangsalId);

        var reservation = ReservationModel.Create(
            pasien,
            request.PlannedDate,
            kelas,
            bangsal,
            request.UserId);

        _reservationRepo.SaveChanges(reservation);

        return Task.FromResult(new AdmCreateReservationResponse(reservation.ReservationId));
    }
}

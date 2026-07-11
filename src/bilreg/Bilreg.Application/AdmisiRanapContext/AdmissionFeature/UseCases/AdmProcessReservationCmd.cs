using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessReservationCmd(
    string ReservationId,
    string KelasDkId,
    string BangsalId,
    string UserId,
    AdmissionRegistrationData Registration) : IRequest<AdmProcessAdmissionResponse>, IReservationKey;

public class AdmProcessReservationHandler : IRequestHandler<AdmProcessReservationCmd, AdmProcessAdmissionResponse>
{
    private readonly IAdmissionRegistrationOrchestrator _orchestrator;

    public AdmProcessReservationHandler(IAdmissionRegistrationOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessReservationCmd request,
        CancellationToken cancellationToken) =>
        _orchestrator.ProcessReservation(request, cancellationToken);
}

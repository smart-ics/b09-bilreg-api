using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessOpnameRequestCmd(
    string OpnameRequestId,
    string KelasDkId,
    string BangsalId,
    string UserId,
    AdmissionRegistrationData Registration) : IRequest<AdmProcessAdmissionResponse>, IOpnameRequestKey;

public class AdmProcessOpnameRequestHandler : IRequestHandler<AdmProcessOpnameRequestCmd, AdmProcessAdmissionResponse>
{
    private readonly IAdmissionRegistrationOrchestrator _orchestrator;

    public AdmProcessOpnameRequestHandler(IAdmissionRegistrationOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessOpnameRequestCmd request,
        CancellationToken cancellationToken) =>
        _orchestrator.ProcessOpnameRequest(request, cancellationToken);
}

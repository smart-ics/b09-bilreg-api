using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessDirectAdmissionCmd(
    string PasienId,
    string KelasDkId,
    string BangsalId,
    string UserId,
    AdmissionRegistrationData Registration,
    string? KelasId = null,
    int Priority = 3) : IRequest<AdmProcessAdmissionResponse>, IPasienKey;

public class AdmProcessDirectAdmissionHandler : IRequestHandler<AdmProcessDirectAdmissionCmd, AdmProcessAdmissionResponse>
{
    private readonly IAdmissionRegistrationOrchestrator _orchestrator;

    public AdmProcessDirectAdmissionHandler(IAdmissionRegistrationOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessDirectAdmissionCmd request,
        CancellationToken cancellationToken) =>
        _orchestrator.ProcessDirectAdmission(request, cancellationToken);
}

using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessInpatientOrderCmd(
    string InpatientOrderId,
    string KelasDkId,
    string BangsalId,
    string UserId,
    AdmissionRegistrationData Registration,
    string? KelasId = null,
    int Priority = 3) : IRequest<AdmProcessAdmissionResponse>, IInpatientOrderKey;

public class AdmProcessInpatientOrderHandler : IRequestHandler<AdmProcessInpatientOrderCmd, AdmProcessAdmissionResponse>
{
    private readonly IAdmissionRegistrationOrchestrator _orchestrator;

    public AdmProcessInpatientOrderHandler(IAdmissionRegistrationOrchestrator orchestrator)
        => _orchestrator = orchestrator;

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessInpatientOrderCmd request,
        CancellationToken cancellationToken) =>
        _orchestrator.ProcessInpatientOrder(request, cancellationToken);
}

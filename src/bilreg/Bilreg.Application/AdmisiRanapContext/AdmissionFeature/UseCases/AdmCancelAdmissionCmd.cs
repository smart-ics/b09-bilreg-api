using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmCancelAdmissionCmd(
    string RegId,
    string UserId) : IRequest, IRegKey;

public class AdmCancelAdmissionHandler : IRequestHandler<AdmCancelAdmissionCmd>
{
    private readonly IAdmissionRepo _admissionRepo;

    public AdmCancelAdmissionHandler(IAdmissionRepo admissionRepo) =>
        _admissionRepo = admissionRepo;

    public Task Handle(AdmCancelAdmissionCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var admission = AdmisiRanapSupport.LoadAdmission(_admissionRepo, request);
        var cancelled = admission.Cancel(request.UserId);

        _admissionRepo.SaveChanges(cancelled);
        return Task.CompletedTask;
    }
}

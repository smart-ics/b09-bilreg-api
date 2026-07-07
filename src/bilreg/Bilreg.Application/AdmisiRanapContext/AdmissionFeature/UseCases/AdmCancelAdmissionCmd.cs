using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

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

        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");
        var cancelled = admission.Cancel(request.UserId);

        _admissionRepo.SaveChanges(cancelled);
        return Task.CompletedTask;
    }
}

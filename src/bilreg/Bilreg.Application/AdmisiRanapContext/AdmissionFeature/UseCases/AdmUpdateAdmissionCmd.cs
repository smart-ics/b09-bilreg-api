using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmUpdateAdmissionCmd(
    string RegId,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest, IRegKey;

public class AdmUpdateAdmissionHandler : IRequestHandler<AdmUpdateAdmissionCmd>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IWardAccommodationGateway _wardGateway;

    public AdmUpdateAdmissionHandler(
        IAdmissionRepo admissionRepo,
        IWardAccommodationGateway wardGateway)
    {
        _admissionRepo = admissionRepo;
        _wardGateway = wardGateway;
    }

    public Task Handle(AdmUpdateAdmissionCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);

        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");
        var updated = admission.Update(kelas, bangsal, request.UserId);

        _admissionRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}

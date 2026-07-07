using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmUpdateAdmissionCmd(
    string RegId,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest, IRegKey;

public class AdmUpdateAdmissionHandler : IRequestHandler<AdmUpdateAdmissionCmd>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public AdmUpdateAdmissionHandler(
        IAdmissionRepo admissionRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo)
    {
        _admissionRepo = admissionRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task Handle(AdmUpdateAdmissionCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelas = AdmisiRanapSupport.LoadKelasReff(_kelasRepo, request.KelasId);
        var bangsal = AdmisiRanapSupport.LoadBangsalReff(_bangsalRepo, request.BangsalId);

        var admission = AdmisiRanapSupport.LoadAdmission(_admissionRepo, request);
        var updated = admission.Update(kelas, bangsal, request.UserId);

        _admissionRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}

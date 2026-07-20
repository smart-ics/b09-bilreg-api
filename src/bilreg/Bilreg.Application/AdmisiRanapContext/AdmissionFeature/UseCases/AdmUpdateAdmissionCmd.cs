using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmUpdateAdmissionCmd(
    string RegId,
    string KelasDkId,
    string BangsalId,
    string UserId) : IRequest, IRegKey;

public class AdmUpdateAdmissionHandler : IRequestHandler<AdmUpdateAdmissionCmd>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmUpdateAdmissionHandler(
        IAdmissionRepo admissionRepo,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _admissionRepo = admissionRepo;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmUpdateAdmissionCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.KelasDkId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelasDk = _wardGateway.ResolveKelasDk(request.KelasDkId);
        var bangsal = _wardGateway.ResolveBangsalForCareClass(request.BangsalId, request.KelasDkId);

        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");
        var snapshotJson = AuditLogSnapshotJson.Serialize(admission);
        var occurredAt = _tglJamProvider.Now;
        var updated = admission.Update(kelasDk, bangsal, request.UserId, occurredAt);

        _admissionRepo.SaveChanges(updated);

        _auditRepo.SaveChanges(AuditLog.Create(
            updated.AuditTrail.Modified,
            "UPDATE",
            nameof(AdmissionModel),
            updated.RegId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}

using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmCancelAdmissionCmd(
    string RegId,
    string UserId) : IRequest, IRegKey;

public class AdmCancelAdmissionHandler : IRequestHandler<AdmCancelAdmissionCmd>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IAuditRepo _auditRepo;

    public AdmCancelAdmissionHandler(
        IAdmissionRepo admissionRepo,
        IAuditRepo auditRepo)
    {
        _admissionRepo = admissionRepo;
        _auditRepo = auditRepo;
    }

    public Task Handle(AdmCancelAdmissionCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");
        var snapshotJson = AuditLogSnapshotJson.Serialize(admission);
        var cancelled = admission.Cancel(request.UserId);

        _admissionRepo.SaveChanges(cancelled);

        _auditRepo.SaveChanges(AuditLog.Create(
            cancelled.AuditTrail.Modified,
            "VOID",
            nameof(AdmissionModel),
            cancelled.RegId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}

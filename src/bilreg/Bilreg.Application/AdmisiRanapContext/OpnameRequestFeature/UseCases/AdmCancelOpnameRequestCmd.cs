using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmCancelOpnameRequestCmd(
    string OpnameRequestId,
    string UserId) : IRequest, IOpnameRequestKey;

public class AdmCancelOpnameRequestHandler : IRequestHandler<AdmCancelOpnameRequestCmd>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;
    private readonly IAuditRepo _auditRepo;

    public AdmCancelOpnameRequestHandler(
        IOpnameRequestRepo opnameRequestRepo,
        IAuditRepo auditRepo)
    {
        _opnameRequestRepo = opnameRequestRepo;
        _auditRepo = auditRepo;
    }

    public Task Handle(AdmCancelOpnameRequestCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OpnameRequestId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var opnameRequest = _opnameRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Opname Request '{request.OpnameRequestId}' tidak ditemukan.");
        var snapshotJson = AuditLogSnapshotJson.Serialize(opnameRequest);
        var cancelled = opnameRequest.Cancel(request.UserId);

        _opnameRequestRepo.SaveChanges(cancelled);

        _auditRepo.SaveChanges(AuditLog.Create(
            cancelled.AuditTrail.Modified,
            "VOID",
            nameof(OpnameRequestModel),
            cancelled.OpnameRequestId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}

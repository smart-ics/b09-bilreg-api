using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmSetInsuranceOpnameRequestCmd(string OpnameRequestId, string TipeJaminanId, 
    string ReffId, string UserId) : IRequest, IOpnameRequestKey, ITipeJaminanKey;

public class AdmSetInsunraceOpnameRequestHandler : IRequestHandler<AdmSetInsuranceOpnameRequestCmd>
{
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IOpnameRequestRepo _opnameRequestRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    public AdmSetInsunraceOpnameRequestHandler(ITipeJaminanRepo tipeJaminanRepo,
        IOpnameRequestRepo opnameRequestRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _tipeJaminanRepo = tipeJaminanRepo;
        _opnameRequestRepo = opnameRequestRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmSetInsuranceOpnameRequestCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OpnameRequestId);
        Guard.Against.NullOrWhiteSpace(request.TipeJaminanId);
        Guard.Against.NullOrWhiteSpace(request.ReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var tipeJmn = _tipeJaminanRepo.LoadEntity(request).GetValueOrThrow($"Tipe Jaminan {request.TipeJaminanId} not found");
        var opmReq = _opnameRequestRepo.LoadEntity(request).GetValueOrThrow($"OpnameRequest {request.OpnameRequestId} not found");
        var occurredAt = _tglJamProvider.Now;
        var snapshotJson = AuditLogSnapshotJson.Serialize(opmReq);

        opmReq.SetInsurace(tipeJmn, request.ReffId, request.UserId, occurredAt);

        _opnameRequestRepo.SaveChanges(opmReq);
        _auditRepo.SaveChanges(AuditLog.Create(
            opmReq.AuditTrail.Modified,
            "MODIF",
            nameof(OpnameRequestModel),
            opmReq.OpnameRequestId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}

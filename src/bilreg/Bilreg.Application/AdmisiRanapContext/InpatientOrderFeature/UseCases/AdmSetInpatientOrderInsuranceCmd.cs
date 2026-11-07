using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmSetInpatientOrderInsuranceCmd(
    string InpatientOrderId,
    string TipeJaminanId,
    string ReffId,
    string UserId) : IRequest, IInpatientOrderKey, ITipeJaminanKey;

public class AdmSetInpatientOrderInsuranceHandler : IRequestHandler<AdmSetInpatientOrderInsuranceCmd>
{
    private readonly IInpatientOrderRepo _orderRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmSetInpatientOrderInsuranceHandler(
        IInpatientOrderRepo orderRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _orderRepo = orderRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmSetInpatientOrderInsuranceCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.InpatientOrderId);
        Guard.Against.NullOrWhiteSpace(request.TipeJaminanId);
        Guard.Against.NullOrWhiteSpace(request.ReffId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var tipeJmn = _tipeJaminanRepo.LoadEntity(request)
            .GetValueOrThrow($"Tipe Jaminan '{request.TipeJaminanId}' not found");

        var order = _orderRepo.GetByKey(request.InpatientOrderId)
            .GetValueOrThrow($"Inpatient order '{request.InpatientOrderId}' not found");

        var occurredAt = _tglJamProvider.Now;
        var snapshotJson = AuditLogSnapshotJson.Serialize(order);

        var updated = order.SetInsurance(tipeJmn, request.ReffId, request.UserId, occurredAt);

        _orderRepo.SaveChanges(updated);

        _auditRepo.SaveChanges(AuditLog.Create(
            updated.AuditTrail.Modified,
            "MODIF",
            nameof(InpatientOrderModel),
            updated.InpatientOrderId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}

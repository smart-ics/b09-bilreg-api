using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmCancelInpatientOrderCmd(
    string InpatientOrderId,
    string UserId,
    string Reason) : IRequest, IInpatientOrderKey;

public class AdmCancelInpatientOrderHandler : IRequestHandler<AdmCancelInpatientOrderCmd>
{
    private readonly IInpatientOrderRepo _orderRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmCancelInpatientOrderHandler(
        IInpatientOrderRepo orderRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _orderRepo = orderRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmCancelInpatientOrderCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.InpatientOrderId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.Reason);

        var order = _orderRepo.GetByKey(request.InpatientOrderId)
            .GetValueOrThrow($"Inpatient order '{request.InpatientOrderId}' tidak ditemukan.");

        var snapshotJson = AuditLogSnapshotJson.Serialize(order);
        var occurredAt = _tglJamProvider.Now;
        var cancelled = order.Cancel(request.UserId, occurredAt);

        _orderRepo.SaveChanges(cancelled);

        _auditRepo.SaveChanges(AuditLog.Create(
            cancelled.AuditTrail.Voided,
            "VOID",
            nameof(InpatientOrderModel),
            cancelled.InpatientOrderId,
            reason: request.Reason,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}

using Ardalis.GuardClauses;
using System.Text.Json;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCancelCmd(string OrderId, string UserId, string Reason,
    string ClientIpAddress = "", string UserAgent = "") : IRequest, ILabOrderKey;

public class LabOrderCancelHandler : IRequestHandler<LabOrderCancelCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabBillingIntegration _labBillingIntegration;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILabOwareOutboundQueueRepo _queueRepo;
    private readonly IBillingLockValidationService _billingLockValidator;
    private readonly IAuditRepo _auditRepo;

    public LabOrderCancelHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ITglJamProvider tglJamProvider,
        ILabOwareOutboundQueueRepo queueRepo,
        IBillingLockValidationService billingLockValidator,
        IAuditRepo auditRepo)
    {
        _labOrderRepo = labOrderRepo;
        _labBillingIntegration = labBillingIntegration;
        _tglJamProvider = tglJamProvider;
        _queueRepo = queueRepo;
        _billingLockValidator = billingLockValidator;
        _auditRepo = auditRepo;
    }

    private class NullAuditRepo : IAuditRepo
    {
        public void SaveChanges(AuditLog entity) { }
    }

    public LabOrderCancelHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ITglJamProvider tglJamProvider,
        ILabOwareOutboundQueueRepo queueRepo)
        : this(labOrderRepo, labBillingIntegration, tglJamProvider, queueRepo, new NullBillingLockValidationService(), new NullAuditRepo())
    {
    }

    public LabOrderCancelHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ITglJamProvider tglJamProvider,
        ILabOwareOutboundQueueRepo queueRepo,
        IBillingLockValidationService billingLockValidator)
        : this(labOrderRepo, labBillingIntegration, tglJamProvider, queueRepo, billingLockValidator, new NullAuditRepo())
    {
    }

    public async Task Handle(LabOrderCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _labOrderRepo.LoadEntity(request)
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        var effectiveRegId = !string.IsNullOrWhiteSpace(order.ExecutionRegId)
            ? order.ExecutionRegId
            : order.Patient.RegId;

        await _billingLockValidator.EnsureCanModifyBillingAsync(effectiveRegId, cancellationToken);

        var occurredAt = _tglJamProvider.Now;

        // EX-03: Specimen Guard
        if (order.LabOrderStatus is LabOrderStatusEnum.Collected
            or LabOrderStatusEnum.Recorded
            or LabOrderStatusEnum.Verified
            or LabOrderStatusEnum.Released)
        {
            throw new InvalidOperationException(
                $"LabOrder {order.OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status {order.LabOrderStatus}).");
        }

        // Financial Coordination with TRK-BILLING
        if (order.LabOrderStatus == LabOrderStatusEnum.Charged)
        {
            _labBillingIntegration.CancelCharge(order.OrderId, effectiveRegId, request.UserId, request.Reason, occurredAt);
        }

        var wasOwareSent = order.OwareStatus == OwareStatusEnum.Sent;

        order.Cancel(request.UserId, request.Reason, occurredAt);

        LabOwareOutboundQueueModel? cancelQueue = null;
        if (wasOwareSent)
        {
            var payloadJson = $"{{\"orderId\":\"{order.OrderId}\"}}";
            cancelQueue = LabOwareOutboundQueueModel.CreatePending(
                order.OrderId,
                payloadJson,
                occurredAt,
                messageType: "LabOrderCancel");
        }

        using var trans = TransHelper.NewScope();
        if (cancelQueue != null)
        {
            _queueRepo.SaveChanges(cancelQueue);
        }
        _labOrderRepo.SaveChanges(order);

        var auditLog = AuditLog.Create(
            request.UserId,
            occurredAt,
            actionType: "VOID",
            entityName: nameof(LabOrderModel),
            entityId: order.OrderId,
            reason: request.Reason,
            originalDataJson: JsonSerializer.Serialize(order),
            correlationId: order.Patient?.RegId ?? "",
            clientIpAddress: request.ClientIpAddress,
            userAgent: request.UserAgent);
        _auditRepo.SaveChanges(auditLog);

        trans.Complete();
    }
}

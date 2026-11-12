using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCancelCmd(string OrderId, string UserId, string Reason)
    : IRequest, ILabOrderKey;

public class LabOrderCancelHandler : IRequestHandler<LabOrderCancelCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabBillingIntegration _labBillingIntegration;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILabOwareOutboundQueueRepo _queueRepo;

    public LabOrderCancelHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ITglJamProvider tglJamProvider,
        ILabOwareOutboundQueueRepo queueRepo)
    {
        _labOrderRepo = labOrderRepo;
        _labBillingIntegration = labBillingIntegration;
        _tglJamProvider = tglJamProvider;
        _queueRepo = queueRepo;
    }

    public Task Handle(LabOrderCancelCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));

        var order = _labOrderRepo.LoadEntity(request)
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

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
            _labBillingIntegration.CancelCharge(order.OrderId, order.Patient.RegId, request.UserId, request.Reason, occurredAt);
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
        trans.Complete();

        return Task.CompletedTask;
    }
}

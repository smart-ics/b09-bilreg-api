using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOwareFeature.UseCases;

public record LabOwareQueueEnqueueCmd(string OrderId, string UserId)
    : IRequest<LabOwareQueueEnqueueResponse>, ILabOrderKey;

public record LabOwareQueueEnqueueResponse(string QueueId);

public class LabOwareQueueEnqueueHandler : IRequestHandler<LabOwareQueueEnqueueCmd, LabOwareQueueEnqueueResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabOwareOutboundQueueRepo _queueRepo;

    public LabOwareQueueEnqueueHandler(ILabOrderRepo labOrderRepo, ILabOwareOutboundQueueRepo queueRepo)
    {
        _labOrderRepo = labOrderRepo;
        _queueRepo = queueRepo;
    }

    public Task<LabOwareQueueEnqueueResponse> Handle(
        LabOwareQueueEnqueueCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(request)
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        order.EnsureCanEnqueueOware();

        var payload = LabOwarePayloadBuilder.Build(order);
        var payloadJson = LabOwarePayloadBuilder.Serialize(payload);
        var technicalNow = DateTime.Now;
        var queue = LabOwareOutboundQueueModel.CreatePending(request.OrderId, payloadJson, technicalNow);

        order.MarkOwarePending(request.UserId, technicalNow);

        LabOwareQueueEnqueueResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _queueRepo.SaveChanges(queue);
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOwareQueueEnqueueResponse(queue.QueueId);
        }

        return Task.FromResult(response);
    }
}

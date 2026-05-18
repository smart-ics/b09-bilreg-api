using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOwareFeature;

public class LabOwareQueueProcessor
{
    public const string WorkerUserId = "OWARE-WORKER";

    private readonly ILabOwareOutboundQueueRepo _queueRepo;
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabOwareIntegration _owareIntegration;

    public LabOwareQueueProcessor(
        ILabOwareOutboundQueueRepo queueRepo,
        ILabOrderRepo labOrderRepo,
        ILabOwareIntegration owareIntegration)
    {
        _queueRepo = queueRepo;
        _labOrderRepo = labOrderRepo;
        _owareIntegration = owareIntegration;
    }

    public LabOwareProcessItemResult ProcessOne(string queueId, string actorUserId)
    {
        var queue = _queueRepo.LoadEntity(LabOwareOutboundQueueModel.Key(queueId))
            .GetValueOrThrow($"Queue '{queueId}' not found");

        queue.MarkProcessing();
        using (var trans = TransHelper.NewScope())
        {
            _queueRepo.SaveChanges(queue);
            trans.Complete();
        }

        var sendResult = _owareIntegration.Send(queue.PayloadJson);

        var order = _labOrderRepo.LoadEntity(new OrderIdKey(queue.OrderId))
            .GetValueOrThrow($"LabOrder '{queue.OrderId}' not found");

        if (sendResult.Success)
        {
            queue.MarkSucceeded();
            order.MarkOwareSent(actorUserId);
        }
        else
        {
            queue.MarkFailed(sendResult.ErrorMessage ?? "OWARE send failed");
            order.MarkOwareFailed(actorUserId);
        }

        using (var trans = TransHelper.NewScope())
        {
            _queueRepo.SaveChanges(queue);
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
        }

        return new LabOwareProcessItemResult(
            queue.QueueId,
            sendResult.Success,
            sendResult.ErrorMessage);
    }

    public LabOwareProcessBatchResult ProcessBatch(int batchSize, string actorUserId)
    {
        var items = _queueRepo.ListProcessable(batchSize).ToList();
        var succeeded = 0;
        var failed = 0;

        foreach (var item in items)
        {
            var result = ProcessOne(item.QueueId, actorUserId);
            if (result.Success)
                succeeded++;
            else
                failed++;
        }

        return new LabOwareProcessBatchResult(items.Count, succeeded, failed);
    }

    private sealed record OrderIdKey(string OrderId) : ILabOrderKey;
}

public record LabOwareProcessItemResult(string QueueId, bool Success, string? ErrorMessage);

public record LabOwareProcessBatchResult(int ProcessedCount, int SucceededCount, int FailedCount);

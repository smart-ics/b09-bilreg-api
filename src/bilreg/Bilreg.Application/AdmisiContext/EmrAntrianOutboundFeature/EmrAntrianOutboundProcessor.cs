using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.Integration;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundProcessor
{
    public const string WorkerUserId = "EMR-ANT-OUTBOX";

    private readonly IEmrAntrianOutboundQueueRepo _queueRepo;
    private readonly IEmrAntrianOutboundIntegration _integration;

    public EmrAntrianOutboundProcessor(
        IEmrAntrianOutboundQueueRepo queueRepo,
        IEmrAntrianOutboundIntegration integration)
    {
        _queueRepo = queueRepo;
        _integration = integration;
    }

    public EmrAntrianProcessItemResult ProcessOne(string queueId, string actorUserId)
    {
        var queue = _queueRepo.LoadEntity(EmrAntrianOutboundQueueModel.Key(queueId))
            .GetValueOrThrow($"Queue '{queueId}' not found");

        if (queue.QueueStatus == EmrAntrianOutboundQueueStatusEnum.Succeeded)
            return new EmrAntrianProcessItemResult(queueId, true, null);

        queue.MarkProcessing();
        using (var trans = TransHelper.NewScope())
        {
            _queueRepo.SaveChanges(queue);
            trans.Complete();
        }

        var sendResult = _integration.Send(queue.MessageType, queue.PayloadJson);
        var technicalNow = DateTime.Now;

        if (sendResult.Success)
            queue.MarkSucceeded(technicalNow);
        else
            queue.MarkFailed(sendResult.ErrorMessage ?? "EMR send failed", technicalNow);

        using (var trans = TransHelper.NewScope())
        {
            _queueRepo.SaveChanges(queue);
            trans.Complete();
        }

        return new EmrAntrianProcessItemResult(queueId, sendResult.Success, sendResult.ErrorMessage);
    }

    public EmrAntrianProcessBatchResult ProcessBatch(int batchSize, string actorUserId)
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

        return new EmrAntrianProcessBatchResult(items.Count, succeeded, failed);
    }
}

public record EmrAntrianProcessItemResult(string QueueId, bool Success, string? ErrorMessage);

public record EmrAntrianProcessBatchResult(int ProcessedCount, int SucceededCount, int FailedCount);

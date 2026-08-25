using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.IntegrationFeature;

public class AptIntegrationWorker
{
    public const string WorkerUserId = "APT-WORKER";

    private readonly IAptIntegrationTaskRepo _repo;
    private readonly IReadOnlyDictionary<AptIntegrationTaskTypeEnum, IAptIntegrationHandler> _handlers;

    public AptIntegrationWorker(
        IAptIntegrationTaskRepo repo,
        IEnumerable<IAptIntegrationHandler> handlers)
    {
        _repo = repo;
        _handlers = handlers.ToDictionary(x => x.TaskType);
    }

    public AptIntegrationProcessItemResult ProcessOne(string integrationTaskId)
    {
        var task = _repo.LoadEntity(AptIntegrationTaskModel.Key(integrationTaskId))
            .GetValueOrThrow($"Integration task '{integrationTaskId}' not found");

        if (task.TaskStatus == AptIntegrationTaskStatusEnum.Succeeded)
            return new AptIntegrationProcessItemResult(task.IntegrationTaskId, true, "idempotent");

        if (task.TaskStatus != AptIntegrationTaskStatusEnum.Pending)
        {
            return new AptIntegrationProcessItemResult(
                task.IntegrationTaskId, false, $"Cannot process status {task.TaskStatus}");
        }

        task.ClaimPending();
        if (!_repo.ClaimPending(task))
        {
            return new AptIntegrationProcessItemResult(
                task.IntegrationTaskId, false, "Claim lost");
        }

        using (var trans = TransHelper.NewScope())
        {
            _repo.SaveChanges(task);
            trans.Complete();
        }

        if (!_handlers.TryGetValue(task.TaskType, out var handler))
        {
            task.MarkFailed($"No handler for {task.TaskType}");
            Persist(task);
            return new AptIntegrationProcessItemResult(task.IntegrationTaskId, false, task.LastError);
        }

        AptIntegrationHandleResult send;
        try
        {
            send = handler.Handle(task);
        }
        catch (Exception ex)
        {
            send = new AptIntegrationHandleResult(false, "", ex.Message);
        }

        if (send.Success)
            task.MarkSucceeded(send.CorrelationId);
        else
            task.MarkFailed(send.ErrorMessage ?? "handler failed");

        Persist(task);
        return new AptIntegrationProcessItemResult(task.IntegrationTaskId, send.Success, task.LastError);
    }

    public AptIntegrationProcessBatchResult ProcessBatch(int batchSize)
    {
        var items = _repo.ListPending(batchSize).ToList();
        var succeeded = 0;
        var failed = 0;
        foreach (var item in items)
        {
            var result = ProcessOne(item.IntegrationTaskId);
            if (result.Success)
                succeeded++;
            else
                failed++;
        }

        return new AptIntegrationProcessBatchResult(items.Count, succeeded, failed);
    }

    private void Persist(AptIntegrationTaskModel task)
    {
        using var trans = TransHelper.NewScope();
        _repo.SaveChanges(task);
        trans.Complete();
    }
}

public record AptIntegrationProcessItemResult(string IntegrationTaskId, bool Success, string? Message);

public record AptIntegrationProcessBatchResult(int ProcessedCount, int SucceededCount, int FailedCount);

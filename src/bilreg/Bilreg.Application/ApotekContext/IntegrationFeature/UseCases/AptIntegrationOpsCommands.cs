using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;

public record AptIntegrationFailureQuery(
    AptIntegrationTaskTypeEnum? TaskType,
    AptIntegrationSourceKindEnum? SourceKind,
    AptIntegrationTaskStatusEnum? Status,
    string? LastErrorContains = null)
    : IRequest<IReadOnlyList<AptIntegrationFailureItem>>;

public record AptIntegrationFailureItem(
    string IntegrationTaskId,
    AptIntegrationTaskTypeEnum TaskType,
    AptIntegrationSourceKindEnum SourceKind,
    string SourceId,
    AptIntegrationTaskStatusEnum Status,
    string LastError,
    string CorrelationId,
    int RetryCount);

public record AptIntegrationRetryCmd(string UserId, string IntegrationTaskId) : IRequest<AptIntegrationProcessItemResult>;

public interface IAptIntegrationOpsDal
{
    IReadOnlyList<AptIntegrationFailureItem> List(AptIntegrationFailureQuery filter);
}

public class AptIntegrationFailureHandler
    : IRequestHandler<AptIntegrationFailureQuery, IReadOnlyList<AptIntegrationFailureItem>>
{
    private readonly IAptIntegrationOpsDal _dal;
    public AptIntegrationFailureHandler(IAptIntegrationOpsDal dal) => _dal = dal;
    public Task<IReadOnlyList<AptIntegrationFailureItem>> Handle(AptIntegrationFailureQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_dal.List(request));
}

public class AptIntegrationRetryHandler : IRequestHandler<AptIntegrationRetryCmd, AptIntegrationProcessItemResult>
{
    private readonly IAptIntegrationTaskRepo _repo;
    private readonly AptIntegrationWorker _worker;
    private readonly IAptAuthorizationPolicy _auth;
    private readonly ILogger<AptIntegrationRetryHandler> _logger;

    public AptIntegrationRetryHandler(
        IAptIntegrationTaskRepo repo,
        AptIntegrationWorker worker,
        IAptAuthorizationPolicy auth,
        ILogger<AptIntegrationRetryHandler>? logger = null)
    {
        _repo = repo;
        _worker = worker;
        _auth = auth;
        _logger = logger ?? NullLogger<AptIntegrationRetryHandler>.Instance;
    }

    public Task<AptIntegrationProcessItemResult> Handle(AptIntegrationRetryCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(AptIntegrationRetryCmd), request.UserId);
        var task = _repo.LoadEntity(AptIntegrationTaskModel.Key(request.IntegrationTaskId))
            .GetValueOrThrow($"Task '{request.IntegrationTaskId}' not found");

        _logger.LogInformation(
            "AptIntegration manual retry requested by {UserId} for task {IntegrationTaskId} type {TaskType} source {SourceKind}:{SourceId} status {TaskStatus}",
            request.UserId,
            task.IntegrationTaskId,
            task.TaskType,
            task.SourceKind,
            task.SourceId,
            task.TaskStatus);

        if (task.TaskStatus == AptIntegrationTaskStatusEnum.Processing)
            task.ReclaimStaleProcessing(DateTime.Now);
        else
            task.AssertCanRetry();
        using (var trans = TransHelper.NewScope())
        {
            if (task.TaskStatus == AptIntegrationTaskStatusEnum.Failed)
                task.PrepareRetry();
            _repo.SaveChanges(task);
            trans.Complete();
        }

        var result = _worker.ProcessOne(task.IntegrationTaskId);
        var after = _repo.LoadEntity(AptIntegrationTaskModel.Key(request.IntegrationTaskId))
            .GetValueOrDefault();

        _logger.LogInformation(
            "AptIntegration manual retry finished task {IntegrationTaskId} success {Success} correlation {CorrelationId} source {SourceKind}:{SourceId} message {Message}",
            request.IntegrationTaskId,
            result.Success,
            after?.CorrelationId ?? "",
            after?.SourceKind,
            after?.SourceId ?? "",
            result.Message ?? "");

        return Task.FromResult(result);
    }
}

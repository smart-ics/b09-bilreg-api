using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;

public record AptIntegrationFailureQuery(
    AptIntegrationTaskTypeEnum? TaskType,
    AptIntegrationSourceKindEnum? SourceKind,
    AptIntegrationTaskStatusEnum? Status)
    : IRequest<IReadOnlyList<AptIntegrationFailureItem>>;

public record AptIntegrationFailureItem(
    string IntegrationTaskId,
    AptIntegrationTaskTypeEnum TaskType,
    string SourceId,
    AptIntegrationTaskStatusEnum Status,
    string LastError,
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

    public AptIntegrationRetryHandler(IAptIntegrationTaskRepo repo, AptIntegrationWorker worker, IAptAuthorizationPolicy auth)
    {
        _repo = repo;
        _worker = worker;
        _auth = auth;
    }

    public Task<AptIntegrationProcessItemResult> Handle(AptIntegrationRetryCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(AptIntegrationRetryCmd), request.UserId);
        var task = _repo.LoadEntity(AptIntegrationTaskModel.Key(request.IntegrationTaskId))
            .GetValueOrThrow($"Task '{request.IntegrationTaskId}' not found");
        task.AssertCanRetry();
        using (var trans = TransHelper.NewScope())
        {
            task.PrepareRetry();
            _repo.SaveChanges(task);
            trans.Complete();
        }
        return Task.FromResult(_worker.ProcessOne(task.IntegrationTaskId));
    }
}

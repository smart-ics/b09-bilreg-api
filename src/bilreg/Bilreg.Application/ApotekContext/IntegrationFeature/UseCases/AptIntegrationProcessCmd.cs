using Bilreg.Application.ApotekContext.Shared;
using MediatR;

namespace Bilreg.Application.ApotekContext.IntegrationFeature.UseCases;

public record AptIntegrationProcessCmd(string UserId, int BatchSize)
    : IRequest<AptIntegrationProcessBatchResult>;

public class AptIntegrationProcessHandler : IRequestHandler<AptIntegrationProcessCmd, AptIntegrationProcessBatchResult>
{
    private const int DefaultBatchSize = 20;

    private readonly AptIntegrationWorker _worker;
    private readonly IAptAuthorizationPolicy _auth;

    public AptIntegrationProcessHandler(AptIntegrationWorker worker, IAptAuthorizationPolicy auth)
    {
        _worker = worker;
        _auth = auth;
    }

    public Task<AptIntegrationProcessBatchResult> Handle(
        AptIntegrationProcessCmd request, CancellationToken cancellationToken)
    {
        _auth.AssertCommandAllowed(nameof(AptIntegrationProcessCmd), request.UserId);
        var batchSize = request.BatchSize is < 1 ? DefaultBatchSize : request.BatchSize;
        return Task.FromResult(_worker.ProcessBatch(batchSize));
    }
}

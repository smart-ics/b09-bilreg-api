using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.UseCases;

public record EmrAntrianOutboundProcessCmd(int? BatchSize, string? UserId)
    : IRequest<EmrAntrianProcessBatchResult>;

public class EmrAntrianOutboundProcessHandler
    : IRequestHandler<EmrAntrianOutboundProcessCmd, EmrAntrianProcessBatchResult>
{
    private const int DefaultBatchSize = 20;

    private readonly EmrAntrianOutboundProcessor _processor;

    public EmrAntrianOutboundProcessHandler(EmrAntrianOutboundProcessor processor)
    {
        _processor = processor;
    }

    public Task<EmrAntrianProcessBatchResult> Handle(
        EmrAntrianOutboundProcessCmd request,
        CancellationToken cancellationToken)
    {
        var batchSize = request.BatchSize is null or < 1 ? DefaultBatchSize : request.BatchSize.Value;
        var actor = string.IsNullOrWhiteSpace(request.UserId)
            ? EmrAntrianOutboundProcessor.WorkerUserId
            : request.UserId;

        var result = _processor.ProcessBatch(batchSize, actor);
        return Task.FromResult(result);
    }
}

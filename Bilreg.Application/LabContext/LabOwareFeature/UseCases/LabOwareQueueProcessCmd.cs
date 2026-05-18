using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.LabContext.LabOwareFeature.UseCases;

public record LabOwareQueueProcessCmd(int? BatchSize, string? UserId)
    : IRequest<LabOwareProcessBatchResult>;

public class LabOwareQueueProcessHandler : IRequestHandler<LabOwareQueueProcessCmd, LabOwareProcessBatchResult>
{
    private const int DefaultBatchSize = 20;

    private readonly LabOwareQueueProcessor _processor;

    public LabOwareQueueProcessHandler(LabOwareQueueProcessor processor)
    {
        _processor = processor;
    }

    public Task<LabOwareProcessBatchResult> Handle(
        LabOwareQueueProcessCmd request,
        CancellationToken cancellationToken)
    {
        var batchSize = request.BatchSize is null or < 1 ? DefaultBatchSize : request.BatchSize.Value;
        var actor = string.IsNullOrWhiteSpace(request.UserId)
            ? LabOwareQueueProcessor.WorkerUserId
            : request.UserId;

        var result = _processor.ProcessBatch(batchSize, actor);
        return Task.FromResult(result);
    }
}

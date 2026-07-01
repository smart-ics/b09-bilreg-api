using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOwareFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOwareFeature.UseCases;

public record LabOwareRetryCmd(string QueueId, string UserId)
    : IRequest<LabOwareRetryResponse>, ILabOwareOutboundQueueKey;

public record LabOwareRetryResponse(bool Success, string? ErrorMessage);

public class LabOwareRetryHandler : IRequestHandler<LabOwareRetryCmd, LabOwareRetryResponse>
{
    private readonly ILabOwareOutboundQueueRepo _queueRepo;
    private readonly LabOwareQueueProcessor _processor;

    public LabOwareRetryHandler(ILabOwareOutboundQueueRepo queueRepo, LabOwareQueueProcessor processor)
    {
        _queueRepo = queueRepo;
        _processor = processor;
    }

    public Task<LabOwareRetryResponse> Handle(LabOwareRetryCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.QueueId, nameof(request.QueueId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var queue = _queueRepo.LoadEntity(request)
            .GetValueOrThrow($"Queue '{request.QueueId}' not found");

        queue.AssertCanManualRetry();

        var result = _processor.ProcessOne(request.QueueId, request.UserId);
        return Task.FromResult(new LabOwareRetryResponse(result.Success, result.ErrorMessage));
    }
}

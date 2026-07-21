using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.EmrAntrianOutboundFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.UseCases;

public record EmrAntrianOutboundRetryCmd(string QueueId, string UserId)
    : IRequest<EmrAntrianOutboundRetryResponse>, IEmrAntrianOutboundQueueKey;

public record EmrAntrianOutboundRetryResponse(bool Success, string? ErrorMessage);

public class EmrAntrianOutboundRetryHandler
    : IRequestHandler<EmrAntrianOutboundRetryCmd, EmrAntrianOutboundRetryResponse>
{
    private readonly IEmrAntrianOutboundQueueRepo _queueRepo;
    private readonly EmrAntrianOutboundProcessor _processor;

    public EmrAntrianOutboundRetryHandler(
        IEmrAntrianOutboundQueueRepo queueRepo,
        EmrAntrianOutboundProcessor processor)
    {
        _queueRepo = queueRepo;
        _processor = processor;
    }

    public Task<EmrAntrianOutboundRetryResponse> Handle(
        EmrAntrianOutboundRetryCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.QueueId, nameof(request.QueueId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var queue = _queueRepo.LoadEntity(request)
            .GetValueOrThrow($"Queue '{request.QueueId}' not found");

        queue.AssertCanManualRetry();

        var result = _processor.ProcessOne(request.QueueId, request.UserId);
        return Task.FromResult(new EmrAntrianOutboundRetryResponse(result.Success, result.ErrorMessage));
    }
}

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Taksaka.Abstractions;
using Taksaka.Engine.Execution;
using Taksaka.Server.SignalR;

namespace Taksaka.Server.SignalR;

public sealed class SignalREventPublisher(
    IHubContext<OperationsHub> hubContext,
    ILogger<SignalREventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : class
    {
        if (domainEvent is JobExecutionCompletedEvent executionEvent)
        {
            await hubContext.Clients.All.SendAsync(
                "JobExecutionCompleted",
                new
                {
                    executionEvent.JobId,
                    executionEvent.WorkerName,
                    executionEvent.IsSuccess,
                    executionEvent.Message,
                    executionEvent.CompletedAt
                },
                cancellationToken);

            logger.LogDebug(
                "Published JobExecutionCompleted for job {JobId} to SignalR clients",
                executionEvent.JobId);
        }
    }
}

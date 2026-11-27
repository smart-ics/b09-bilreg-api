using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record StartCountingCommand(string SessionId) : IRequest<StartCountingResponse>;

public record StartCountingResponse(
    string SessionId,
    SessionStatusEnum Status);

public class StartCountingHandler : IRequestHandler<StartCountingCommand, StartCountingResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public StartCountingHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<StartCountingResponse> Handle(
        StartCountingCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        session.StartCounting();
        _sessionRepo.SaveChanges(session);

        return Task.FromResult(new StartCountingResponse(session.SessionId, session.Status));
    }
}

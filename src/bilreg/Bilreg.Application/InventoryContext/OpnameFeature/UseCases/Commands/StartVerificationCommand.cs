using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record StartVerificationCommand(string SessionId) : IRequest<StartVerificationResponse>;

public record StartVerificationResponse(
    string SessionId,
    SessionStatusEnum Status);

public class StartVerificationHandler : IRequestHandler<StartVerificationCommand, StartVerificationResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public StartVerificationHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<StartVerificationResponse> Handle(
        StartVerificationCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        session.StartVerification();
        _sessionRepo.SaveChanges(session);

        return Task.FromResult(new StartVerificationResponse(session.SessionId, session.Status));
    }
}

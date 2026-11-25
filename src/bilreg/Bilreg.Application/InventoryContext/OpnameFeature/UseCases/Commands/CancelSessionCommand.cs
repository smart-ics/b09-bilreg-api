using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record CancelSessionCommand(
    string SessionId,
    string Reason,
    string CancelledBy,
    DateTime? CancelledAt = null) : IRequest<CancelSessionResponse>;

public record CancelSessionResponse(
    string SessionId,
    SessionStatusEnum Status,
    string CancelReason,
    string CancelledBy,
    DateTime CancelledAt);

public class CancelSessionHandler : IRequestHandler<CancelSessionCommand, CancelSessionResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public CancelSessionHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<CancelSessionResponse> Handle(
        CancelSessionCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.Reason, nameof(request.Reason));
        Guard.Against.NullOrWhiteSpace(request.CancelledBy, nameof(request.CancelledBy));

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        session.Cancel(request.Reason, request.CancelledBy, request.CancelledAt);
        _sessionRepo.SaveChanges(session);

        return Task.FromResult(new CancelSessionResponse(
            session.SessionId,
            session.Status,
            session.CancelReason,
            session.CancelledBy,
            session.CancelledAt!.Value));
    }
}

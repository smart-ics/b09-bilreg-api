using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record SubmitVerificationCommand(
    string SessionId,
    string VerifiedBy,
    DateTime? VerifiedAt = null) : IRequest<SubmitVerificationResponse>;

public record SubmitVerificationResponse(
    string SessionId,
    SessionStatusEnum Status,
    string VerifiedBy,
    DateTime? VerifiedAt);

public class SubmitVerificationHandler : IRequestHandler<SubmitVerificationCommand, SubmitVerificationResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public SubmitVerificationHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<SubmitVerificationResponse> Handle(
        SubmitVerificationCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.VerifiedBy, nameof(request.VerifiedBy));

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        session.SubmitVerification(request.VerifiedBy, request.VerifiedAt);
        _sessionRepo.SaveChanges(session);

        return Task.FromResult(new SubmitVerificationResponse(
            session.SessionId,
            session.Status,
            session.VerifiedBy,
            session.VerifiedAt));
    }
}

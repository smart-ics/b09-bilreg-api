using System.ComponentModel.DataAnnotations;
using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record RejectSessionCommand(
    string SessionId,
    RejectionDispositionEnum Disposition,
    string Notes,
    string RejectedBy,
    DateTime? RejectedAt = null) : IRequest<RejectSessionResponse>;

public record RejectSessionResponse(
    string SessionId,
    SessionStatusEnum Status,
    RejectionDispositionEnum Disposition,
    string RevisionNotes);

public class RejectSessionHandler : IRequestHandler<RejectSessionCommand, RejectSessionResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;

    public RejectSessionHandler(IStockOpnameSessionRepo sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    public Task<RejectSessionResponse> Handle(
        RejectSessionCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.Notes, nameof(request.Notes));
        Guard.Against.NullOrWhiteSpace(request.RejectedBy, nameof(request.RejectedBy));

        if (!Enum.IsDefined(typeof(RejectionDispositionEnum), request.Disposition))
            throw new ValidationException("Rejection disposition is invalid.");

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        var session = maybe.Value;
        session.Reject(request.Disposition, request.Notes, request.RejectedBy, request.RejectedAt);

        _sessionRepo.SaveChanges(session);

        return Task.FromResult(new RejectSessionResponse(
            session.SessionId,
            session.Status,
            request.Disposition,
            session.RevisionNotes));
    }
}

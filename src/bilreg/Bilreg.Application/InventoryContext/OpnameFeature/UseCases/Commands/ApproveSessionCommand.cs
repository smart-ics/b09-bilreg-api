using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.OpnameFeature.EventHandlers;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;

public record ApproveSessionCommand(
    string SessionId,
    string ApprovedBy,
    DateTime? ApprovedAt = null) : IRequest<ApproveSessionResponse>;

public record ApproveSessionResponse(
    string SessionId,
    SessionStatusEnum Status,
    string ApprovedBy,
    DateTime? ApprovedAt,
    decimal SurplusValuation,
    decimal DeficitValuation,
    decimal NetValuation,
    int ApprovedItemsCount);

public class ApproveSessionHandler : IRequestHandler<ApproveSessionCommand, ApproveSessionResponse>
{
    private readonly IStockOpnameSessionRepo _sessionRepo;
    private readonly IMediator _mediator;

    public ApproveSessionHandler(
        IStockOpnameSessionRepo sessionRepo,
        IMediator mediator)
    {
        _sessionRepo = sessionRepo;
        _mediator = mediator;
    }

    public async Task<ApproveSessionResponse> Handle(
        ApproveSessionCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.SessionId, nameof(request.SessionId));
        Guard.Against.NullOrWhiteSpace(request.ApprovedBy, nameof(request.ApprovedBy));

        var maybe = _sessionRepo.LoadEntity(new StockOpnameSessionKey(request.SessionId));
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"Stock opname session with ID '{request.SessionId}' not found.");

        using var trans = TransHelper.NewScope();

        var session = maybe.Value;
        var domainEvent = session.Approve(request.ApprovedBy, request.ApprovedAt);

        _sessionRepo.SaveChanges(session);

        // TD-09: Publish event to trigger downstream stock adjustment
        await _mediator.Publish(new StockOpnameApprovedNotification(domainEvent), cancellationToken);

        trans.Complete();

        return new ApproveSessionResponse(
            session.SessionId,
            session.Status,
            session.ApprovedBy,
            session.ApprovedAt,
            session.SurplusValuation,
            session.DeficitValuation,
            session.NetValuation,
            domainEvent.Items.Count);
    }
}

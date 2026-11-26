using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.OpnameFeature.EventHandlers;

public record StockOpnameApprovedNotification(
    string SessionId,
    string LayananId,
    string ApprovedBy,
    DateTime ApprovedAt,
    IReadOnlyList<StockOpnameApprovedItem> Items) : INotification
{
    public StockOpnameApprovedNotification(StockOpnameApprovedEvent domainEvent)
        : this(
            Guard.Against.Null(domainEvent, nameof(domainEvent)).SessionId,
            domainEvent.LayananId,
            domainEvent.ApprovedBy,
            domainEvent.ApprovedAt,
            domainEvent.Items)
    {
    }

    public StockOpnameApprovedEvent ToDomainEvent() =>
        new(SessionId, LayananId, ApprovedBy, ApprovedAt, Items);
}

public class StockOpnameApprovedEventHandler :
    INotificationHandler<StockOpnameApprovedNotification>
{
    private readonly IMediator _mediator;

    public StockOpnameApprovedEventHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Handle(StockOpnameApprovedNotification notification, CancellationToken cancellationToken)
    {
        Guard.Against.Null(notification, nameof(notification));
        await DispatchAdjustmentCommandAsync(
            notification.SessionId,
            notification.LayananId,
            notification.ApprovedBy,
            notification.Items,
            cancellationToken);
    }

    public async Task<PostStockAdjustmentConsequenceResult> HandleAsync(
        StockOpnameApprovedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(domainEvent, nameof(domainEvent));
        return await DispatchAdjustmentCommandAsync(
            domainEvent.SessionId,
            domainEvent.LayananId,
            domainEvent.ApprovedBy,
            domainEvent.Items,
            cancellationToken);
    }

    private async Task<PostStockAdjustmentConsequenceResult> DispatchAdjustmentCommandAsync(
        string sessionId,
        string layananId,
        string approvedBy,
        IReadOnlyList<StockOpnameApprovedItem> items,
        CancellationToken cancellationToken)
    {
        var adjustmentItems = (items ?? [])
            .Where(x => x.AdjustmentQty != 0)
            .Select(x => new StockAdjustmentItemRequest(
                BrgId: x.BrgId,
                BatchNo: x.BatchNo,
                ExpiredDate: x.ExpiredDate,
                AdjustmentQty: x.AdjustmentQty,
                Hpp: x.Hpp,
                SatuanId: x.SatuanId,
                ExecutedBy: approvedBy))
            .ToList();

        var command = new PostStockAdjustmentConsequenceCommand(
            SessionId: sessionId,
            LayananId: layananId,
            Items: adjustmentItems);

        return await _mediator.Send(command, cancellationToken);
    }
}

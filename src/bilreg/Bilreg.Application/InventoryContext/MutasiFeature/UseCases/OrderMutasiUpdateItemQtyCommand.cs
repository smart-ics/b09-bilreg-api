using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiUpdateItemQtyCommand(
    string OrderMutasiId,
    string BrgId,
    decimal Qty,
    string UserId) : IRequest, IOrderMutasiKey, IBrgKey;

public class OrderMutasiUpdateItemQtyHandler : IRequestHandler<OrderMutasiUpdateItemQtyCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiUpdateItemQtyHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task Handle(OrderMutasiUpdateItemQtyCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);
        Guard.Against.NullOrWhiteSpace(request.BrgId);
        Guard.Against.Negative(request.Qty);

        // BUILD
        var order = GetOrderMutasi(request);
        order.SetItemQty(new BrgKey(request.BrgId), request.Qty, request.UserId);

        // PERSIST
        _orderMutasiRepo.SaveChanges(order);
        return Task.CompletedTask;
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiUpdateItemQtyCommand key)
    {
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"OrderMutasi {key.OrderMutasiId} not found");

        return maybe.Value;
    }
}

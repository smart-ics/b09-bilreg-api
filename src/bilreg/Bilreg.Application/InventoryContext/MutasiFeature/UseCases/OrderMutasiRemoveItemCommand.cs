using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiRemoveItemCommand(
    string OrderMutasiId,
    string BrgId,
    string UserId) : IRequest, IOrderMutasiKey, IBrgKey;

public class OrderMutasiRemoveItemHandler : IRequestHandler<OrderMutasiRemoveItemCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiRemoveItemHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task Handle(OrderMutasiRemoveItemCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);
        Guard.Against.NullOrWhiteSpace(request.BrgId);

        // BUILD
        var order = GetOrderMutasi(request);
        order.RemoveItem(new BrgKey(request.BrgId), request.UserId);

        // PERSIST
        _orderMutasiRepo.SaveChanges(order);
        return Task.CompletedTask;
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiRemoveItemCommand key)
    {
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"OrderMutasi {key.OrderMutasiId} not found");

        return maybe.Value;
    }
}

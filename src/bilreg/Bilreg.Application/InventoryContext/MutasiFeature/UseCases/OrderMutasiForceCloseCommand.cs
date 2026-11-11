using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiForceCloseCommand(
    string OrderMutasiId,
    string Reason,
    string UserId) : IRequest, IOrderMutasiKey;

public class OrderMutasiForceCloseHandler : IRequestHandler<OrderMutasiForceCloseCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiForceCloseHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task Handle(OrderMutasiForceCloseCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);
        Guard.Against.NullOrWhiteSpace(request.Reason);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // BUILD
        var order = GetOrderMutasi(request);
        order.ForceClose(request.Reason, request.UserId);

        // PERSIST
        _orderMutasiRepo.SaveChanges(order);
        return Task.CompletedTask;
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiForceCloseCommand key)
    {
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"OrderMutasi {key.OrderMutasiId} not found");

        return maybe.Value;
    }
}

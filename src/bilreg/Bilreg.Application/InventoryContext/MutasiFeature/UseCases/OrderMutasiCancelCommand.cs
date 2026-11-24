using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiCancelCommand(
    string OrderMutasiId,
    string Reason,
    string UserId) : IRequest, IOrderMutasiKey;

public class OrderMutasiCancelHandler : IRequestHandler<OrderMutasiCancelCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiCancelHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task Handle(OrderMutasiCancelCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // BUILD
        var order = GetOrderMutasi(request);
        order.Cancel(request.Reason, request.UserId);

        // PERSIST
        _orderMutasiRepo.SaveChanges(order);
        return Task.CompletedTask;
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiCancelCommand key)
    {
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"OrderMutasi {key.OrderMutasiId} not found");

        return maybe.Value;
    }
}

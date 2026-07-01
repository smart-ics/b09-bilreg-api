using Ardalis.GuardClauses;
using Farinv.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiRejectCommand(string OrderMutasiId, string Note, string UserId) 
    : IRequest, IOrderMutasiKey;

public class OrderMutasiRejectHandler : IRequestHandler<OrderMutasiRejectCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiRejectHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task Handle(OrderMutasiRejectCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // BUILD
        var order = GetOrderMutasi(request);
        order.Reject(request.Note, request.UserId);

        // PERSIST
        _orderMutasiRepo.SaveChanges(order);

        return Task.CompletedTask;
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiRejectCommand key)
    {
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"OrderMutasi {key.OrderMutasiId} not found");

        return maybe.Value;
    }
}
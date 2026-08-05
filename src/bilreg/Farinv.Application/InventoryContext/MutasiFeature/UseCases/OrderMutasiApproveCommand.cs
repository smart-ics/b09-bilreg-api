using Ardalis.GuardClauses;
using Farinv.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiApproveCommand(string OrderMutasiId, string UserId) : IRequest, IOrderMutasiKey;

public class OrderMutasiApproveHandler : IRequestHandler<OrderMutasiApproveCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiApproveHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task Handle(OrderMutasiApproveCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // BUILD
        var order = GetOrderMutasi(request);
        order.Approve(request.UserId);

        // PERSIST
        _orderMutasiRepo.SaveChanges(order);

        return Task.CompletedTask;
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiApproveCommand key)
    {
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new KeyNotFoundException($"OrderMutasi {key.OrderMutasiId} not found");
        
        return maybe.Value;
    }
}


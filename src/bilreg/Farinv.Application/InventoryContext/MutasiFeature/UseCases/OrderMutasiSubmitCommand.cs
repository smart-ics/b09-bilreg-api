using Ardalis.GuardClauses;
using Farinv.Application.InventoryContext.StokFeature;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using MediatR;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiSubmitCommand(string OrderMutasiId,
    string LayananTujuanId, string UserId) : IRequest, IOrderMutasiKey;

public class OrderMutasiSubmitHandler : IRequestHandler<OrderMutasiSubmitCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;
    private readonly ILayananRepo _layananRepo;

    public OrderMutasiSubmitHandler(IOrderMutasiRepo orderMutasiRepo,
        ILayananRepo layananRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
        _layananRepo = layananRepo;
    }

    public Task Handle(OrderMutasiSubmitCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);
        Guard.Against.NullOrWhiteSpace(request.LayananTujuanId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // BUILD       
        var order = GetOrderMutasi(request);
        var tujuan = GetLayananTujuan(request.LayananTujuanId);
        order.Submit(tujuan, request.UserId);     

        // WRITE
        _orderMutasiRepo.SaveChanges(order);
        return Task.CompletedTask;
    }

    private LayananReff GetLayananTujuan(string layananTujuanId)
    {
        var layananKey = LayananType.Key(layananTujuanId);
        var layananMaybe = _layananRepo.LoadEntity(layananKey);
        if (!layananMaybe.HasValue)
            throw new KeyNotFoundException($"LayananTujuan {layananTujuanId} not found");

        var layanan = layananMaybe.Value.ToReff();
        return layanan;
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiSubmitCommand request)
    {
        var orderMaybe = _orderMutasiRepo.LoadEntity(request);
        if (!orderMaybe.HasValue)
            throw new KeyNotFoundException($"Order Mutasi {request.OrderMutasiId} not found");

        var order = orderMaybe.Value; 
        if (order.State != OrderMutasiStateEnum.Draft) 
            throw new ArgumentException($"OrderMutasi already Submitted"); 

        return order;

    }
}
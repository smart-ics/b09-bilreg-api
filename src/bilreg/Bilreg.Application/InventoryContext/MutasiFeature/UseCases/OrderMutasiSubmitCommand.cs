using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiSubmitCommand(
    string OrderMutasiId,
    string LayananTujuanId,
    string UserId,
    OrderMutasiPriorityEnum Priority = OrderMutasiPriorityEnum.Rutin,
    string OrderNote = "-",
    bool AutoApprove = false) : IRequest, IOrderMutasiKey;

public class OrderMutasiSubmitHandler : IRequestHandler<OrderMutasiSubmitCommand>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;
    private readonly ILayananRepo _layananRepo;

    public OrderMutasiSubmitHandler(
        IOrderMutasiRepo orderMutasiRepo,
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
        order.Submit(tujuan, request.UserId, request.Priority, request.OrderNote, request.AutoApprove);

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

        return layananMaybe.Value.ToReff();
    }

    private OrderMutasiModel GetOrderMutasi(OrderMutasiSubmitCommand request)
    {
        var orderMaybe = _orderMutasiRepo.LoadEntity(request);
        if (!orderMaybe.HasValue)
            throw new KeyNotFoundException($"Order Mutasi {request.OrderMutasiId} not found");

        return orderMaybe.Value;
    }
}

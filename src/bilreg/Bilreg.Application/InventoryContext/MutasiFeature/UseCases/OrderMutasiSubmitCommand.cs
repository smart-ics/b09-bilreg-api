using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

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
    private readonly ITglJamProvider? _tglJamProvider;

    public OrderMutasiSubmitHandler(
        IOrderMutasiRepo orderMutasiRepo,
        ILayananRepo layananRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _orderMutasiRepo = orderMutasiRepo;
        _layananRepo = layananRepo;
        _tglJamProvider = tglJamProvider;
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
        var now = _tglJamProvider?.Now ?? DateTime.Now;
        order.Submit(tujuan, request.UserId, request.Priority, request.OrderNote, request.AutoApprove, now);

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

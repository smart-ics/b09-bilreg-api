using Ardalis.GuardClauses;
using Farinv.Application.InventoryContext.StokFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using MediatR;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiDraftStateGetQuery(string LayananOrderId) 
    : IRequest<OrderMutasiDraftStateGetResponse>;

public record OrderMutasiDraftStateGetResponse(string OrderMutasiId, string OrderMutasiDate,
    string State, LayananReff LayananOrder, LayananReff LayananTujuan, string OrderNote,
    IEnumerable<OrderMutasiDraftStateItemGetResponse> ListItem);

public record OrderMutasiDraftStateItemGetResponse(int NoUrut, BrgReff Brg, decimal Qty, SatuanType Satuan);

public class OrderMutasiDraftStateGetHandler 
    : IRequestHandler<OrderMutasiDraftStateGetQuery, OrderMutasiDraftStateGetResponse>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;
    private readonly ILayananRepo _layananRepo;

    public OrderMutasiDraftStateGetHandler(IOrderMutasiRepo orderMutasiRepo,
        ILayananRepo layananRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
        _layananRepo = layananRepo;
    }

    public Task<OrderMutasiDraftStateGetResponse> Handle(OrderMutasiDraftStateGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.LayananOrderId);

        var layanan = GetLayananOrder(request.LayananOrderId);
        var order = GetOrderDraftState(layanan);

        return Task.FromResult(GenResponse(order));
    }

    #region PRIVATE-HELPERS
    private static OrderMutasiDraftStateGetResponse GenResponse(OrderMutasiModel order)
    {
        var items = order.ListItem
            .Select(x => new OrderMutasiDraftStateItemGetResponse(x.NoUrut, x.Brg, x.Qty, x.Satuan))
            ?.ToList() ?? [];

        var result = new OrderMutasiDraftStateGetResponse(order.OrderMutasiId,
            order.OrderMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"), order.State.ToString(),
            order.LayananOrder, order.LayananTujuan, order.OrderNote, items);
        return result;
    }

    private OrderMutasiModel GetOrderDraftState(LayananType layanan)
    {
        var header = _orderMutasiRepo.ListDraftState()
           .FirstOrDefault(x => x.LayananOrder.LayananId == layanan.LayananId) 
           ?? throw new KeyNotFoundException($"OrderMutasi with Draft State not found");

        var key = OrderMutasiModel.Key(header.OrderMutasiId);
        var maybe = _orderMutasiRepo.LoadEntity(key);
        if (!maybe.HasValue)
            throw new ArgumentException($"OrderMutasiId {header.OrderMutasiId} not found");

        return maybe.Value;
    }

    private LayananType GetLayananOrder(string layananOrderId)
    {
        var layananKey = LayananType.Key(layananOrderId);
        var layananMaybe = _layananRepo.LoadEntity(layananKey);
        if (!layananMaybe.HasValue)
            throw new KeyNotFoundException($"LayananOrder {layananOrderId} not found");

        return layananMaybe.Value;
    }
    #endregion
}
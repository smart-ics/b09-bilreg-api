using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiDraftStateGetQuery(string LayananOrderId) 
    : IRequest<OrderMutasiDraftStateGetResponse>;

public record OrderMutasiDraftStateGetResponse(
    string OrderMutasiId,
    string OrderMutasiDate,
    string State,
    LayananReff LayananOrder,
    LayananReff LayananTujuan,
    string OrderNote,
    IEnumerable<OrderMutasiDraftStateItemGetResponse> ListItem,
    string Priority = "Rutin");

public record OrderMutasiDraftStateItemGetResponse(
    int NoUrut,
    BrgReff Brg,
    decimal StokCurrent,
    decimal Qty,
    SatuanType Satuan);

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
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananOrderId, nameof(request.LayananOrderId));

        var layanan = GetLayananOrder(request.LayananOrderId);
        var order = GetOrderDraftState(layanan);
        if (order is null)
        {
            var defaultResponse = new OrderMutasiDraftStateGetResponse(
                "-",
                string.Empty,
                "Draft",
                layanan.ToReff(),
                LayananType.Default.ToReff(),
                "-",
                [],
                "Rutin");
            return Task.FromResult(defaultResponse);
        }

        return Task.FromResult(GenResponse(order));
    }

    #region PRIVATE-HELPERS
    private static OrderMutasiDraftStateGetResponse GenResponse(OrderMutasiModel order)
    {
        var items = order.ListItem
            .Select(x => new OrderMutasiDraftStateItemGetResponse(x.NoUrut, x.Brg, x.StokCurrent, x.Qty, x.Satuan))
            .ToList();

        var result = new OrderMutasiDraftStateGetResponse(
            order.OrderMutasiId,
            order.OrderMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"),
            order.State.ToString(),
            order.LayananOrder,
            order.LayananTujuan,
            order.OrderNote,
            items,
            order.Priority.ToString());
        return result;
    }

    private OrderMutasiModel? GetOrderDraftState(LayananType layanan)
    {
        var header = _orderMutasiRepo.ListDraftState()
           ?.FirstOrDefault(x => x.LayananOrder.LayananId == layanan.LayananId);

        if (header is null)
            return null;

        var key = OrderMutasiModel.Key(header.OrderMutasiId);
        var maybe = _orderMutasiRepo.LoadEntity(key);
        return maybe.HasValue ? maybe.Value : null;
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

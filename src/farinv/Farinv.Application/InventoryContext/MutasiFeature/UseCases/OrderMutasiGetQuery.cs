using Ardalis.GuardClauses;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using MediatR;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiGetQuery(string OrderMutasiId) 
    : IRequest<OrderMutasiGetResponse>, IOrderMutasiKey;

public record OrderMutasiGetResponse(string OrderMutasiId, string OrderMutasiDate, 
    string State, LayananReff LayananOrder, LayananReff LayananTujuan,
    OrderMutasiApprovalGetResponse Approval, OrderMutasiApprovalGetResponse Rejection, string OrderNote, 
    IEnumerable<OrderMutasiItemGetResponse> ListItem);

public record OrderMutasiApprovalGetResponse(string UserId, string Date);

public record OrderMutasiItemGetResponse(int NoUrut, BrgReff Brg, decimal Qty, SatuanType Satuan);

public class OrderMutasiGetHandler : IRequestHandler<OrderMutasiGetQuery, OrderMutasiGetResponse>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiGetHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task<OrderMutasiGetResponse> Handle(OrderMutasiGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderMutasiId);

        var order = _orderMutasiRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"OrderMutasi {request.OrderMutasiId} not found")
            );

        return Task.FromResult(GenResponse(order));
    }

    private static OrderMutasiGetResponse GenResponse(OrderMutasiModel order)
    {
        var items = order.ListItem
            .Select(x => new OrderMutasiItemGetResponse(x.NoUrut, x.Brg, x.Qty, x.Satuan))
            ?.ToList() ?? [];

        var approval = new OrderMutasiApprovalGetResponse(order.Approval.UserId, order.Approval.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        var rejection = new OrderMutasiApprovalGetResponse(order.Rejection.UserId, order.Rejection.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

        var result = new OrderMutasiGetResponse(order.OrderMutasiId, 
            order.OrderMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"), order.State.ToString(), 
            order.LayananOrder, order.LayananTujuan, approval, rejection, order.OrderNote, items);
        return result;
    }
}
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record OrderTdkCancelCmd(string OrderTdkId, string UserId) : IRequest, IOrderTdkKey;

public class OrderTdkCancelHandler : IRequestHandler<OrderTdkCancelCmd>
{
    private readonly IOrderTdkRepo _orderTdkrepo;

    public OrderTdkCancelHandler(IOrderTdkRepo orderTdkrepo)
    {
        _orderTdkrepo = orderTdkrepo;
    }

    public Task Handle(OrderTdkCancelCmd request, CancellationToken cancellationToken)
    {
        var order = _orderTdkrepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Order Tindakan {request.OrderTdkId} not found")
            );

        order.Cancel(request.UserId);
        _orderTdkrepo.SaveChanges(order);
        return Task.CompletedTask;
    }
}

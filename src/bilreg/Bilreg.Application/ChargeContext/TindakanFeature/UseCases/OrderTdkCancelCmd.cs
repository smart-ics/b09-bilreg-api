using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record OrderTdkCancelCmd(string OrderTdkId, string UserId) : IRequest, IOrderTdkKey;

public class OrderTdkCancelHandler : IRequestHandler<OrderTdkCancelCmd>
{
    private readonly IOrderTdkRepo _orderTdkrepo;
    private readonly ITglJamProvider _tglJamProvider;

    public OrderTdkCancelHandler(IOrderTdkRepo orderTdkrepo, ITglJamProvider? tglJamProvider = null)
    {
        _orderTdkrepo = orderTdkrepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(OrderTdkCancelCmd request, CancellationToken cancellationToken)
    {
        var order = _orderTdkrepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Order Tindakan {request.OrderTdkId} not found")
            );

        order.Cancel(request.UserId, _tglJamProvider.Now);
        _orderTdkrepo.SaveChanges(order);
        return Task.CompletedTask;
    }
}

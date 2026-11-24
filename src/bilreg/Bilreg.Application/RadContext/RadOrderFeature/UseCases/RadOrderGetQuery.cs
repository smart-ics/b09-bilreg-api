using Ardalis.GuardClauses;
using Bilreg.Domain.RadContext.RadOrderFeature;
using MediatR;

namespace Bilreg.Application.RadContext.RadOrderFeature.UseCases;

public record RadOrderGetQuery(string OrderId) : IRequest<RadOrderDetailResponse?>, IRadOrderKey;

public record RadOrderDetailResponse(RadOrderDto Header, IEnumerable<RadOrderItemDto> Items);

public class RadOrderGetHandler : IRequestHandler<RadOrderGetQuery, RadOrderDetailResponse?>
{
    private readonly IRadOrderDal _orderDal;
    private readonly IRadOrderItemDal _itemDal;

    public RadOrderGetHandler(IRadOrderDal orderDal, IRadOrderItemDal itemDal)
    {
        _orderDal = orderDal;
        _itemDal = itemDal;
    }

    public Task<RadOrderDetailResponse?> Handle(RadOrderGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));

        var header = _orderDal.GetData(request);
        if (header is null)
            return Task.FromResult<RadOrderDetailResponse?>(null);

        var items = _itemDal.ListData(request);
        var response = new RadOrderDetailResponse(header, items);
        return Task.FromResult<RadOrderDetailResponse?>(response);
    }
}

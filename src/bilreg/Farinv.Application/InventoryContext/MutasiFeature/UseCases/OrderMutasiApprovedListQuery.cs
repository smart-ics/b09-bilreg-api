using Ardalis.GuardClauses;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiApprovedListQuery(string TglYmd1, string TglYmd2)
    : IRequest<IEnumerable<OrderMutasiApprovedListResponse>>;

public record OrderMutasiApprovedListResponse(string OrderMutasiId,
    string OrderMutasiDate, string State,
    LayananReff LayananOrder, LayananReff LayananTujuan);

public class OrderMutasiApprovedListHandler
    : IRequestHandler<OrderMutasiApprovedListQuery, IEnumerable<OrderMutasiApprovedListResponse>>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiApprovedListHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task<IEnumerable<OrderMutasiApprovedListResponse>> Handle(OrderMutasiApprovedListQuery request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.TglYmd1, nameof(request.TglYmd1));
        Guard.Against.InvalidDateFormat(request.TglYmd2, nameof(request.TglYmd2));

        var listApproved = ListApproved(request);

        return Task.FromResult(GenResponse(listApproved));
    }

    private static IEnumerable<OrderMutasiApprovedListResponse> GenResponse(List<OrderMutasiHeaderView> listApproved)
    {
        var result = listApproved
            .Select(x => new OrderMutasiApprovedListResponse(x.OrderMutasiId,
                x.OrderMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"), x.State.ToString(), x.LayananOrder, x.LayananTujuan));
        return result;
    }

    private List<OrderMutasiHeaderView> ListApproved(OrderMutasiApprovedListQuery request)
    {
        var tgl1 = request.TglYmd1.ToDate("yyyy-MM-dd");
        var tgl2 = request.TglYmd2.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl1, tgl2);
        var listOrder = _orderMutasiRepo.ListApprovedState(periode)?.ToList() ?? [];

        return listOrder;
    }
}
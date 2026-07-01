using Ardalis.GuardClauses;
using Farinv.Domain.InventoryContext.MutasiFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Farinv.Application.InventoryContext.MutasiFeature.UseCases;

public record OrderMutasiSubmittedListQuery(string TglYmd1, string TglYmd2) 
    : IRequest<IEnumerable<OrderMutasiSubmittedListResponse>>;

public record OrderMutasiSubmittedListResponse(string OrderMutasiId,
    string OrderMutasiDate, string State, 
    LayananReff LayananOrder, LayananReff LayananTujuan);

public class OrderMutasiSubmitedListHandler
    : IRequestHandler<OrderMutasiSubmittedListQuery, IEnumerable<OrderMutasiSubmittedListResponse>>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiSubmitedListHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task<IEnumerable<OrderMutasiSubmittedListResponse>> Handle(OrderMutasiSubmittedListQuery request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.TglYmd1, nameof(request.TglYmd1));
        Guard.Against.InvalidDateFormat(request.TglYmd2, nameof(request.TglYmd2));

        var listSubmited = ListSubmited(request);

        return Task.FromResult(GenResponse(listSubmited));
    }

    private static IEnumerable<OrderMutasiSubmittedListResponse> GenResponse(IEnumerable<OrderMutasiHeaderView> listSubmited)
    {
        var result = listSubmited
            .Select(x => new OrderMutasiSubmittedListResponse(x.OrderMutasiId,
                x.OrderMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"), x.State.ToString(), x.LayananOrder, x.LayananTujuan));
        return result;
    }

    private IEnumerable<OrderMutasiHeaderView> ListSubmited(OrderMutasiSubmittedListQuery request)
    {
        var tgl1 = request.TglYmd1.ToDate("yyyy-MM-dd");
        var tgl2 = request.TglYmd2.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl1, tgl2);
        var listOrder = _orderMutasiRepo.ListData(periode)?.ToList() ?? [];
        var listSubmited = listOrder.Where(x => x.State == OrderMutasiStateEnum.Submitted);

        return listSubmited;
    }
}
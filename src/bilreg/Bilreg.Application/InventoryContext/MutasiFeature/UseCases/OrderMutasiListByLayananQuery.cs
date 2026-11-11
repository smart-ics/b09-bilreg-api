using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record LayananKey(string LayananId) : ILayananKey;

public record OrderMutasiListByLayananQuery(string LayananOrderId, string TglYmd1, string TglYmd2)
    : IRequest<IEnumerable<OrderMutasiListByLayananResponse>>, ILayananKey
{
    public string LayananId => LayananOrderId;
}

public record OrderMutasiListByLayananResponse(
    string OrderMutasiId,
    string OrderMutasiDate,
    string State,
    string Priority,
    LayananReff LayananOrder,
    LayananReff LayananTujuan,
    string OrderNote);

public class OrderMutasiListByLayananHandler
    : IRequestHandler<OrderMutasiListByLayananQuery, IEnumerable<OrderMutasiListByLayananResponse>>
{
    private readonly IOrderMutasiRepo _orderMutasiRepo;

    public OrderMutasiListByLayananHandler(IOrderMutasiRepo orderMutasiRepo)
    {
        _orderMutasiRepo = orderMutasiRepo;
    }

    public Task<IEnumerable<OrderMutasiListByLayananResponse>> Handle(
        OrderMutasiListByLayananQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananOrderId, nameof(request.LayananOrderId));
        Guard.Against.InvalidDateFormat(request.TglYmd1, nameof(request.TglYmd1));
        Guard.Against.InvalidDateFormat(request.TglYmd2, nameof(request.TglYmd2));

        var tgl1 = request.TglYmd1.ToDate("yyyy-MM-dd");
        var tgl2 = request.TglYmd2.ToDate("yyyy-MM-dd");
        var periode = new Periode(tgl1, tgl2);

        var list = _orderMutasiRepo.ListData(new LayananKey(request.LayananOrderId), periode)?.ToList() ?? [];

        return Task.FromResult(GenResponse(list));
    }

    private static IEnumerable<OrderMutasiListByLayananResponse> GenResponse(IEnumerable<OrderMutasiHeaderView> list)
    {
        var result = list
            .Select(x => new OrderMutasiListByLayananResponse(
                x.OrderMutasiId,
                x.OrderMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"),
                x.State.ToString(),
                x.Priority.ToString(),
                x.LayananOrder,
                x.LayananTujuan,
                x.OrderNote));
        return result;
    }
}

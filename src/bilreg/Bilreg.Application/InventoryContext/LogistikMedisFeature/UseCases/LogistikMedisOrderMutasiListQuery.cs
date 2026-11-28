using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;

public record LogistikMedisOrderMutasiListQuery(string LayananOrderId, string TglYmd1, string TglYmd2)
    : IRequest<IEnumerable<LogistikMedisOrderMutasiListResponse>>;

public record LogistikMedisOrderMutasiListResponse(
    string OrderMutasiId,
    string OrderMutasiDate,
    string State,
    string Priority,
    string LayananOrderId,
    string LayananOrderName,
    string LayananTujuanId,
    string LayananTujuanName,
    string OrderNote,
    int TotalItem,
    decimal TotalQty,
    decimal TotalQtyTerpenuhi,
    bool CanCancel,
    bool CanForceClose,
    IReadOnlyList<LogistikMedisOrderMutasiItemResponse> ListItem
);

public record LogistikMedisOrderMutasiItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    decimal StokCurrent,
    decimal Qty,
    decimal QtyTerpenuhi,
    string SatuanId,
    string SatuanName
);

public class LogistikMedisOrderMutasiListHandler
    : IRequestHandler<LogistikMedisOrderMutasiListQuery, IEnumerable<LogistikMedisOrderMutasiListResponse>>
{
    private readonly ILogistikMedisBffDal _bffDal;

    public LogistikMedisOrderMutasiListHandler(ILogistikMedisBffDal bffDal)
    {
        _bffDal = bffDal;
    }

    public Task<IEnumerable<LogistikMedisOrderMutasiListResponse>> Handle(
        LogistikMedisOrderMutasiListQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananOrderId, nameof(request.LayananOrderId));
        Guard.Against.InvalidDateFormat(request.TglYmd1, nameof(request.TglYmd1));
        Guard.Against.InvalidDateFormat(request.TglYmd2, nameof(request.TglYmd2));

        var layananOrderId = request.LayananOrderId.Trim();
        var tgl1 = request.TglYmd1.ToDate("yyyy-MM-dd").Date;
        var tgl2 = request.TglYmd2.ToDate("yyyy-MM-dd").Date.AddDays(1).AddSeconds(-1);

        var headers = (_bffDal.ListOrderMutasiByLayananOrder(layananOrderId, tgl1, tgl2) ?? [])
            .Where(x => x.State != OrderMutasiStateEnum.Draft)
            .OrderByDescending(x => x.OrderMutasiDate)
            .ToList();

        if (headers.Count == 0)
            return Task.FromResult<IEnumerable<LogistikMedisOrderMutasiListResponse>>([]);

        var ids = headers.Select(x => x.OrderMutasiId).ToList();
        var itemsLookup = (_bffDal.ListOrderMutasiItems(ids) ?? [])
            .ToLookup(x => x.OrderMutasiId, StringComparer.OrdinalIgnoreCase);

        var result = headers.Select(h =>
        {
            var listItem = itemsLookup[h.OrderMutasiId]
                .OrderBy(i => i.NoUrut)
                .Select(i => new LogistikMedisOrderMutasiItemResponse(
                    NoUrut: i.NoUrut,
                    BrgId: i.BrgId,
                    BrgName: i.BrgName,
                    StokCurrent: i.StokCurrent,
                    Qty: i.Qty,
                    QtyTerpenuhi: i.QtyTerpenuhi,
                    SatuanId: i.SatuanId,
                    SatuanName: i.SatuanName))
                .ToList();

            var canCancel = (h.State == OrderMutasiStateEnum.Submitted || h.State == OrderMutasiStateEnum.Approved)
                && listItem.All(x => x.QtyTerpenuhi == 0m);
            var canForceClose = h.State == OrderMutasiStateEnum.PartiallyFulfilled;

            return new LogistikMedisOrderMutasiListResponse(
                OrderMutasiId: h.OrderMutasiId,
                OrderMutasiDate: h.OrderMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"),
                State: h.State.ToString(),
                Priority: h.Priority.ToString(),
                LayananOrderId: h.LayananOrderId,
                LayananOrderName: h.LayananOrderName,
                LayananTujuanId: h.LayananTujuanId,
                LayananTujuanName: h.LayananTujuanName,
                OrderNote: h.OrderNote ?? string.Empty,
                TotalItem: listItem.Count,
                TotalQty: listItem.Sum(x => x.Qty),
                TotalQtyTerpenuhi: listItem.Sum(x => x.QtyTerpenuhi),
                CanCancel: canCancel,
                CanForceClose: canForceClose,
                ListItem: listItem);
        }).ToList();

        return Task.FromResult<IEnumerable<LogistikMedisOrderMutasiListResponse>>(result);
    }
}

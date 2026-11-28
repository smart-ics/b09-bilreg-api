using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;

public record LogistikMedisMutasiKeluarListQuery(string LayananAsalId, string TglYmd1, string TglYmd2)
    : IRequest<IEnumerable<LogistikMedisMutasiKeluarListResponse>>;

public record LogistikMedisMutasiKeluarListResponse(
    string MutasiBrgId,
    string MutasiBrgDate,
    string State,
    string LayananAsalId,
    string LayananAsalName,
    string LayananTujuanId,
    string LayananTujuanName,
    string OrderMutasiId,
    string DispatchNote,
    string IssuerUserId,
    string CancelReason,
    int TotalItem,
    decimal TotalQty,
    bool CanCancel,
    IReadOnlyList<LogistikMedisMutasiBrgItemResponse> ListItem
);

public class LogistikMedisMutasiKeluarListHandler
    : IRequestHandler<LogistikMedisMutasiKeluarListQuery, IEnumerable<LogistikMedisMutasiKeluarListResponse>>
{
    private readonly ILogistikMedisBffDal _bffDal;

    public LogistikMedisMutasiKeluarListHandler(ILogistikMedisBffDal bffDal)
    {
        _bffDal = bffDal;
    }

    public Task<IEnumerable<LogistikMedisMutasiKeluarListResponse>> Handle(
        LogistikMedisMutasiKeluarListQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananAsalId, nameof(request.LayananAsalId));
        Guard.Against.InvalidDateFormat(request.TglYmd1, nameof(request.TglYmd1));
        Guard.Against.InvalidDateFormat(request.TglYmd2, nameof(request.TglYmd2));

        var layananAsalId = request.LayananAsalId.Trim();
        var tgl1 = request.TglYmd1.ToDate("yyyy-MM-dd").Date;
        var tgl2 = request.TglYmd2.ToDate("yyyy-MM-dd").Date.AddDays(1).AddSeconds(-1);

        var headers = (_bffDal.ListMutasiBrgKeluar(layananAsalId, tgl1, tgl2) ?? [])
            .OrderByDescending(x => x.MutasiBrgDate)
            .ToList();

        if (headers.Count == 0)
            return Task.FromResult<IEnumerable<LogistikMedisMutasiKeluarListResponse>>([]);

        var ids = headers.Select(x => x.MutasiBrgId).ToList();
        var itemsLookup = (_bffDal.ListMutasiBrgItems(ids) ?? [])
            .ToLookup(x => x.MutasiBrgId, StringComparer.OrdinalIgnoreCase);

        var result = headers.Select(h =>
        {
            var listItem = itemsLookup[h.MutasiBrgId]
                .OrderBy(i => i.NoUrut)
                .Select(i => new LogistikMedisMutasiBrgItemResponse(
                    NoUrut: i.NoUrut,
                    BrgId: i.BrgId,
                    BrgName: i.BrgName,
                    BatchNo: i.BatchNo ?? string.Empty,
                    ExpiredDate: i.ExpiredDate ?? string.Empty,
                    QtyKirim: i.QtyKirim,
                    SatuanId: i.SatuanId,
                    SatuanName: i.SatuanName))
                .ToList();

            return new LogistikMedisMutasiKeluarListResponse(
                MutasiBrgId: h.MutasiBrgId,
                MutasiBrgDate: h.MutasiBrgDate.ToString("yyyy-MM-dd HH:mm:ss"),
                State: h.State.ToString(),
                LayananAsalId: h.LayananAsalId,
                LayananAsalName: h.LayananAsalName,
                LayananTujuanId: h.LayananTujuanId,
                LayananTujuanName: h.LayananTujuanName,
                OrderMutasiId: h.OrderMutasiId ?? string.Empty,
                DispatchNote: h.DispatchNote ?? string.Empty,
                IssuerUserId: h.IssuerUserId ?? string.Empty,
                CancelReason: h.CancelReason ?? string.Empty,
                TotalItem: listItem.Count,
                TotalQty: listItem.Sum(x => x.QtyKirim),
                CanCancel: h.State == MutasiBrgStateEnum.Dispatched,
                ListItem: listItem);
        }).ToList();

        return Task.FromResult<IEnumerable<LogistikMedisMutasiKeluarListResponse>>(result);
    }
}

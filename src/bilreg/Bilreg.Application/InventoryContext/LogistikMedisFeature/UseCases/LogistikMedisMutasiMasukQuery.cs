using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;

public record LogistikMedisMutasiMasukQuery(string LayananTujuanId, string TglYmd1, string TglYmd2)
    : IRequest<LogistikMedisMutasiMasukResponse>;

public record LogistikMedisMutasiMasukResponse(
    IReadOnlyList<LogistikMedisMutasiInTransitResponse> InTransitList,
    IReadOnlyList<LogistikMedisTerimaMutasiResponse> ReceivedHistoryList
);

public record LogistikMedisMutasiInTransitResponse(
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
    int TotalItem,
    decimal TotalQty,
    IReadOnlyList<LogistikMedisMutasiBrgItemResponse> ListItem
);

public record LogistikMedisTerimaMutasiResponse(
    string TerimaMutasiId,
    string TerimaMutasiDate,
    string State,
    string MutasiBrgId,
    string OrderMutasiId,
    string LayananAsalId,
    string LayananAsalName,
    string LayananTujuanId,
    string LayananTujuanName,
    string ReceiverUserId,
    string Note,
    int TotalItem,
    decimal TotalQty,
    IReadOnlyList<LogistikMedisTerimaMutasiItemResponse> ListItem
);

public record LogistikMedisMutasiBrgItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyKirim,
    string SatuanId,
    string SatuanName
);

public record LogistikMedisTerimaMutasiItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyTerima,
    string SatuanId,
    string SatuanName
);

public class LogistikMedisMutasiMasukHandler
    : IRequestHandler<LogistikMedisMutasiMasukQuery, LogistikMedisMutasiMasukResponse>
{
    private readonly ILogistikMedisBffDal _bffDal;

    public LogistikMedisMutasiMasukHandler(ILogistikMedisBffDal bffDal)
    {
        _bffDal = bffDal;
    }

    public Task<LogistikMedisMutasiMasukResponse> Handle(
        LogistikMedisMutasiMasukQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananTujuanId, nameof(request.LayananTujuanId));
        Guard.Against.InvalidDateFormat(request.TglYmd1, nameof(request.TglYmd1));
        Guard.Against.InvalidDateFormat(request.TglYmd2, nameof(request.TglYmd2));

        var layananTujuanId = request.LayananTujuanId.Trim();
        var tgl1 = request.TglYmd1.ToDate("yyyy-MM-dd").Date;
        var tgl2 = request.TglYmd2.ToDate("yyyy-MM-dd").Date.AddDays(1).AddSeconds(-1);

        var inTransitHeaders = (_bffDal.ListMutasiBrgInTransit(layananTujuanId) ?? [])
            .Where(x => x.State == MutasiBrgStateEnum.Dispatched)
            .OrderByDescending(x => x.MutasiBrgDate)
            .ToList();

        ILookup<string, LogistikMedisMutasiBrgItemRowDto> inTransitItemsLookup =
            Array.Empty<LogistikMedisMutasiBrgItemRowDto>().ToLookup(x => x.MutasiBrgId);
        if (inTransitHeaders.Count > 0)
        {
            var inTransitIds = inTransitHeaders.Select(x => x.MutasiBrgId).ToList();
            inTransitItemsLookup = (_bffDal.ListMutasiBrgItems(inTransitIds) ?? [])
                .ToLookup(x => x.MutasiBrgId, StringComparer.OrdinalIgnoreCase);
        }

        var inTransitList = inTransitHeaders.Select(h =>
        {
            var listItem = inTransitItemsLookup[h.MutasiBrgId]
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

            return new LogistikMedisMutasiInTransitResponse(
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
                TotalItem: listItem.Count,
                TotalQty: listItem.Sum(x => x.QtyKirim),
                ListItem: listItem);
        }).ToList();

        var receivedHeaders = (_bffDal.ListTerimaMutasi(layananTujuanId, tgl1, tgl2) ?? [])
            .OrderByDescending(x => x.TerimaMutasiDate)
            .ToList();

        ILookup<string, LogistikMedisTerimaMutasiItemRowDto> terimaItemsLookup =
            Array.Empty<LogistikMedisTerimaMutasiItemRowDto>().ToLookup(x => x.TerimaMutasiId);
        if (receivedHeaders.Count > 0)
        {
            var terimaIds = receivedHeaders.Select(x => x.TerimaMutasiId).ToList();
            terimaItemsLookup = (_bffDal.ListTerimaMutasiItems(terimaIds) ?? [])
                .ToLookup(x => x.TerimaMutasiId, StringComparer.OrdinalIgnoreCase);
        }

        var receivedHistoryList = receivedHeaders.Select(h =>
        {
            var listItem = terimaItemsLookup[h.TerimaMutasiId]
                .OrderBy(i => i.NoUrut)
                .Select(i => new LogistikMedisTerimaMutasiItemResponse(
                    NoUrut: i.NoUrut,
                    BrgId: i.BrgId,
                    BrgName: i.BrgName,
                    BatchNo: i.BatchNo ?? string.Empty,
                    ExpiredDate: i.ExpiredDate ?? string.Empty,
                    QtyTerima: i.QtyTerima,
                    SatuanId: i.SatuanId,
                    SatuanName: i.SatuanName))
                .ToList();

            return new LogistikMedisTerimaMutasiResponse(
                TerimaMutasiId: h.TerimaMutasiId,
                TerimaMutasiDate: h.TerimaMutasiDate.ToString("yyyy-MM-dd HH:mm:ss"),
                State: h.State.ToString(),
                MutasiBrgId: h.MutasiBrgId,
                OrderMutasiId: h.OrderMutasiId ?? string.Empty,
                LayananAsalId: h.LayananAsalId,
                LayananAsalName: h.LayananAsalName,
                LayananTujuanId: h.LayananTujuanId,
                LayananTujuanName: h.LayananTujuanName,
                ReceiverUserId: h.ReceiverUserId ?? string.Empty,
                Note: h.Note ?? string.Empty,
                TotalItem: listItem.Count,
                TotalQty: listItem.Sum(x => x.QtyTerima),
                ListItem: listItem);
        }).ToList();

        return Task.FromResult(new LogistikMedisMutasiMasukResponse(inTransitList, receivedHistoryList));
    }
}

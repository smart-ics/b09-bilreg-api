using MediatR;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryListPendingPoQuery(
    string? TglAwal = null,
    string? TglAkhir = null,
    string? Search = null,
    int Limit = 100
) : IRequest<IEnumerable<DeliveryPendingPoResponse>>;

public record DeliveryPendingPoResponse(
    string PoId,
    string PoDate,
    string SupplierId,
    string SupplierName,
    string LayananId,
    string LayananName,
    string Keterangan,
    decimal StatusDo,
    IReadOnlyList<DeliveryPendingPoItemResponse> Items
);

public record DeliveryPendingPoItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal QtyPo,
    decimal QtyDo,
    decimal SisaQty,
    decimal Konversi,
    decimal Harga,
    decimal Diskon
);

public class DeliveryListPendingPoHandler : IRequestHandler<DeliveryListPendingPoQuery, IEnumerable<DeliveryPendingPoResponse>>
{
    private readonly IPoDeliveryTrackingDal _poDeliveryTrackingDal;

    public DeliveryListPendingPoHandler(IPoDeliveryTrackingDal poDeliveryTrackingDal)
    {
        _poDeliveryTrackingDal = poDeliveryTrackingDal;
    }

    public Task<IEnumerable<DeliveryPendingPoResponse>> Handle(DeliveryListPendingPoQuery request, CancellationToken cancellationToken)
    {
        var pendingItems = _poDeliveryTrackingDal.ListPendingPoItems(request.TglAwal, request.TglAkhir, request.Search, request.Limit) ?? [];

        var response = pendingItems
            .GroupBy(x => new
            {
                x.PoId,
                x.PoDate,
                x.SupplierId,
                x.SupplierName,
                x.LayananId,
                x.LayananName,
                x.Keterangan,
                x.StatusDo
            })
            .Select(g => new DeliveryPendingPoResponse(
                g.Key.PoId,
                g.Key.PoDate,
                g.Key.SupplierId,
                g.Key.SupplierName,
                g.Key.LayananId,
                g.Key.LayananName,
                g.Key.Keterangan,
                g.Key.StatusDo,
                g.Select(i => new DeliveryPendingPoItemResponse(
                    i.NoUrut,
                    i.BrgId,
                    i.BrgName,
                    i.SatuanId,
                    i.SatuanName,
                    i.QtyPo,
                    i.QtyDo,
                    i.SisaQty,
                    i.Konversi,
                    i.Harga,
                    i.Diskon
                )).ToList()
            ));

        return Task.FromResult(response);
    }
}

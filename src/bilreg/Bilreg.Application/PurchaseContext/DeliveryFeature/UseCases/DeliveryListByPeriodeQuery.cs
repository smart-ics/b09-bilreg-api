using Ardalis.GuardClauses;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryListByPeriodeQuery(string TglYmdAwal, string TglYmdAkhir)
    : IRequest<IEnumerable<DeliveryListByPeriodeResponse>>;

public record DeliveryListByPeriodeResponse(
    string DeliveryOrderId,
    string DeliveryOrderDate,
    string SuratJalanNo,
    string PoId,
    string LayananId,
    string LayananName,
    string SupplierId,
    string SupplierName,
    string Keterangan,
    string Status,
    decimal Total
);

public class DeliveryListByPeriodeHandler : IRequestHandler<DeliveryListByPeriodeQuery, IEnumerable<DeliveryListByPeriodeResponse>>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;

    public DeliveryListByPeriodeHandler(IDeliveryOrderRepo deliveryOrderRepo)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
    }

    public Task<IEnumerable<DeliveryListByPeriodeResponse>> Handle(DeliveryListByPeriodeQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TglYmdAwal);
        Guard.Against.NullOrWhiteSpace(request.TglYmdAkhir);

        var tglAwal = request.TglYmdAwal.ToDate("yyyy-MM-dd");
        var tglAkhir = request.TglYmdAkhir.ToDate("yyyy-MM-dd");
        var periode = new Periode(tglAwal, tglAkhir);

        var listDeliveryOrder = _deliveryOrderRepo.ListData(periode)
            .GetValueOrDefault([]);

        var response = listDeliveryOrder.Select(x => new DeliveryListByPeriodeResponse(
            x.DeliveryOrderId,
            x.DeliveryOrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
            x.SuratJalanNo,
            x.PoId,
            x.Gudang.LayananId,
            x.Gudang.LayananName,
            x.Supplier.PartnerId,
            x.Supplier.PartnerName,
            x.Keterangan,
            x.Status.ToString(),
            x.Total
        ));

        return Task.FromResult(response);
    }
}

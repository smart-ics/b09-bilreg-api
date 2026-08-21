using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.PurchaseOrderFeature.UseCases;

public record PurchaseOrderListByPeriodeQuery(string TglYmdAwal, string TglYmdAkhir)
    : IRequest<IEnumerable<PurchaseOrderListByPeriodeResponse>>;

public record PurchaseOrderListByPeriodeResponse(
    string PurchaseOrderId,
    string PurchaseOrderDate,
    string Keterangan,
    string PartnerId,
    string PartnerName,
    decimal SubTotal,
    decimal TaxTotal,
    decimal Total,
    decimal DiskonLain,
    decimal BiayaLain,
    decimal GrandTotal
);

public class PurchaseOrderListByPeriodeHandler : IRequestHandler<PurchaseOrderListByPeriodeQuery,
    IEnumerable<PurchaseOrderListByPeriodeResponse>>
{
    private readonly IPurchaseOrderRepo _purchaseOrderRepo;

    public PurchaseOrderListByPeriodeHandler(IPurchaseOrderRepo purchaseOrderRepo)
    {
        _purchaseOrderRepo = purchaseOrderRepo;
    }

    public Task<IEnumerable<PurchaseOrderListByPeriodeResponse>> Handle(PurchaseOrderListByPeriodeQuery request,
        CancellationToken cancellationToken)
    {
        var tglawal = request.TglYmdAwal.ToDate("yyyy-MM-dd");
        var tglAkhir = request.TglYmdAkhir.ToDate("yyyy-MM-dd");
        var periode = new Periode(tglawal, tglAkhir);

        var listPurchaseOrder = _purchaseOrderRepo.ListData(periode)
            .GetValueOrDefault([]);
        
        var response = listPurchaseOrder.Select(x => new PurchaseOrderListByPeriodeResponse(
            x.PurchaseOrderId,
            x.AuditTrail.Created.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            x.Keterangan,
            x.Partner.PartnerId,
            x.Partner.PartnerName,
            x.SubTotal,
            x.TaxTotal,
            x.Total,
            x.DiskonLain,
            x.BiayaLain,
            x.GrandTotal
        ));

        return Task.FromResult(response);
    }
}
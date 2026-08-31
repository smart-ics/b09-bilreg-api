using Ardalis.GuardClauses;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using MediatR;

namespace Bilreg.Application.PurchaseContext.PurchaseOrderFeature.UseCases;

public record PurchaseOrderGetQuery(string PurchaseOrderId) : IRequest<PurchaseOrderGetResponse>, IPurchaseOrderKey;

public record PurchaseOrderGetResponse(
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
    decimal GrandTotal,
    IReadOnlyList<PurchaseOrderGetItemResponse> ListItem
);

public record PurchaseOrderGetItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal Harga,
    decimal QtyStok,
    decimal Qty,
    decimal SubTotal,
    decimal DiskonPercentage,
    decimal DiskonTotal,
    decimal TaxPercentage,
    decimal TaxTotal,
    decimal BiayaLain,
    decimal Total
);

public class PurchaseOrderGetHandler : IRequestHandler<PurchaseOrderGetQuery, PurchaseOrderGetResponse>
{
    private readonly IPurchaseOrderRepo _purchaseOrderRepo;

    public PurchaseOrderGetHandler(IPurchaseOrderRepo purchaseOrderRepo)
    {
        _purchaseOrderRepo = purchaseOrderRepo;
    }

    public Task<PurchaseOrderGetResponse> Handle(PurchaseOrderGetQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.PurchaseOrderId);
        
        // BUILD
        var purchaseOrder = _purchaseOrderRepo.LoadEntity(request)
            .GetValueOrThrow($"Purchase Order with Id: {request.PurchaseOrderId} not found.");
        
        // RESPONSE
        var response = BuildResponse(purchaseOrder);
        return Task.FromResult(response);
    }

    private static PurchaseOrderGetResponse BuildResponse(PurchaseOrderModel model)
    {
        var listItem = model.ListItem.Select(x => new PurchaseOrderGetItemResponse(
            x.NoUrut,
            x.Brg.BrgId,
            x.Brg.BrgName,
            x.Satuan.SatuanId,
            x.Satuan.SatuanName,
            x.Harga,
            x.QtyStok,
            x.Qty,
            x.Subtotal,
            x.DiskonPercentage,
            x.DiskonTotal,
            x.TaxPercentage,
            x.TaxTotal,
            x.BiayaLain,
            x.Total
        )).ToList();

        var response = new PurchaseOrderGetResponse(
            model.PurchaseOrderId,
            model.AuditTrail.Created.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
            model.Keterangan,
            model.Partner.PartnerId,
            model.Partner.PartnerName,
            model.SubTotal,
            model.TaxTotal,
            model.Total,
            model.DiskonLain,
            model.BiayaLain,
            model.GrandTotal,
            listItem
        );

        return response;
    }
}
using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using MediatR;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryGetQuery(string DeliveryOrderId) : IRequest<DeliveryGetResponse>, IDeliveryOrderKey;

public record DeliveryGetResponse(
    string DeliveryOrderId,
    string SuratJalanNo,
    string PoId,
    string DeliveryOrderDate,
    PartnerReff Supplier,
    LayananReff Gudang,
    string Keterangan,
    string Status,
    decimal SubTotal,
    decimal TaxTotal,
    decimal DiskonTotal,
    decimal Total,
    decimal BiayaLain,
    decimal DiskonLain,
    decimal GrandTotal,
    DeliveryGetConfirmedResponse Confirmed,
    IReadOnlyList<DeliveryGetItemResponse> ListItem,
    DeliveryGetAuditResponse Created,
    string PoDate = ""
)
{
    public DeliveryGetResponse(
        string deliveryOrderId,
        string suratJalanNo,
        string poId,
        string deliveryOrderDate,
        PartnerReff supplier,
        LayananReff gudang,
        string keterangan,
        string status,
        decimal subTotal,
        decimal taxTotal,
        decimal diskonTotal,
        decimal total,
        decimal biayaLain,
        decimal diskonLain,
        decimal grandTotal,
        DeliveryGetConfirmedResponse confirmed,
        IReadOnlyList<DeliveryGetItemResponse> listItem)
        : this(deliveryOrderId, suratJalanNo, poId, deliveryOrderDate, supplier, gudang, keterangan, status,
               subTotal, taxTotal, diskonTotal, total, biayaLain, diskonLain, grandTotal, confirmed, listItem,
               new DeliveryGetAuditResponse(string.Empty, string.Empty), string.Empty)
    {
    }
}

public record DeliveryGetAuditResponse(
    string UserId,
    string Timestamp,
    string UserName = ""
);

public record DeliveryGetConfirmedResponse(
    string UserId,
    string Timestamp,
    string UserName = ""
);

public record DeliveryGetItemResponse(
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    int Konversi,
    decimal Harga,
    decimal Diskon,
    decimal SubTotal,
    decimal TaxPercentage,
    decimal Tax,
    decimal Total,
    string NoBatch,
    string TglEd,
    DeliveryGetInspectionResponse Inspection,
    decimal Het = 0m
);

public record DeliveryGetInspectionResponse(
    decimal QtyKirim,
    decimal QtyDiterima,
    decimal QtyDitolak,
    string AlasanTolak,
    bool IsBonus,
    bool HasJaminanRetur,
    string NoSuratJaminan,
    string PoId,
    int PoNoUrut
);

public class DeliveryGetHandler : IRequestHandler<DeliveryGetQuery, DeliveryGetResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;

    public DeliveryGetHandler(IDeliveryOrderRepo deliveryOrderRepo)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
    }

    public Task<DeliveryGetResponse> Handle(DeliveryGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId);

        var deliveryOrder = _deliveryOrderRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Delivery Order with ID '{request.DeliveryOrderId}' not found.")
            );

        var response = BuildResponse(deliveryOrder);
        return Task.FromResult(response);
    }

    private static DeliveryGetResponse BuildResponse(DeliveryOrderModel model)
    {
        var listItem = model.ListItem.Select(item => new DeliveryGetItemResponse(
            item.NoUrut,
            item.Brg.BrgId,
            item.Brg.BrgName,
            item.Satuan.SatuanId,
            item.Satuan.SatuanName,
            item.Konversi,
            item.Harga,
            item.Diskon,
            item.SubTotal,
            item.TaxPercentage,
            item.Tax,
            item.Total,
            item.NoBatch,
            item.TglEd.ToString("yyyy-MM-dd"),
            new DeliveryGetInspectionResponse(
                item.Inspection.QtyKirim,
                item.Inspection.QtyDiterima,
                item.Inspection.QtyDitolak,
                item.Inspection.AlasanTolak,
                item.Inspection.IsBonus,
                item.Inspection.HasJaminanRetur,
                item.Inspection.NoSuratJaminan,
                item.Inspection.PoId,
                item.Inspection.PoNoUrut
            ),
            item.Het
        )).ToList();

        var confirmedAtStr = model.Confirmed.Timestamp != default && model.Confirmed.Timestamp.Year < 3000
            ? model.Confirmed.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            : string.Empty;

        var createdStr = model.AuditTrail?.Created is { } created && created.Timestamp != default && created.Timestamp.Year < 3000
            ? created.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            : string.Empty;
        var createdUser = model.AuditTrail?.Created?.UserId ?? string.Empty;

        return new DeliveryGetResponse(
            model.DeliveryOrderId,
            model.SuratJalanNo,
            model.PoId,
            model.DeliveryOrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
            model.Supplier,
            model.Gudang,
            model.Keterangan,
            model.Status.ToString(),
            model.SubTotal,
            model.TaxTotal,
            model.DiskonTotal,
            model.Total,
            model.BiayaLain,
            model.DiskonLain,
            model.GrandTotal,
            new DeliveryGetConfirmedResponse(model.Confirmed.UserId, confirmedAtStr, model.ConfirmedUserName),
            listItem,
            new DeliveryGetAuditResponse(createdUser, createdStr, model.CreatedUserName),
            model.PoDate
        );
    }
}

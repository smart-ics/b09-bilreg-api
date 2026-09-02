using Ardalis.GuardClauses;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using MediatR;

namespace Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;

public record DeliveryOrderGetQry(string DeliveryOrderId)
    : IRequest<DeliveryOrderGetResponse>, IDeliveryOrderKey;

public record DeliveryOrderGetResponse(
    string DeliveryOrderId,
    string DoNo,
    string SupplierId,
    string SupplierName,
    string PoReffId,
    DateTime DoDate,
    DeliveryOrderStateEnum State,
    string Notes,
    DeliveryOrderAuditResponse Audit,
    IReadOnlyCollection<DeliveryOrderItemResponse> ListItem);

public record DeliveryOrderAuditResponse(
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate);

public record DeliveryOrderItemResponse(
    int ItemNo,
    string BrgId,
    string LayananId,
    decimal QtyOrder,
    decimal QtyReceived,
    decimal QtyRemaining,
    string SatuanId,
    decimal Harga,
    decimal Diskon,
    decimal Tax,
    DateTime TglEd,
    string NoBatch,
    DeliveryOrderItemStateEnum State);

public class DeliveryOrderGetHandler
    : IRequestHandler<DeliveryOrderGetQry, DeliveryOrderGetResponse>
{
    private readonly IDeliveryOrderRepo _deliveryOrderRepo;

    public DeliveryOrderGetHandler(IDeliveryOrderRepo deliveryOrderRepo)
    {
        _deliveryOrderRepo = deliveryOrderRepo;
    }

    public Task<DeliveryOrderGetResponse> Handle(
        DeliveryOrderGetQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DeliveryOrderId);
        var maybe = _deliveryOrderRepo.LoadEntity(request);
        if (!maybe.HasValue)
            throw new KeyNotFoundException(
                $"DeliveryOrderId '{request.DeliveryOrderId}' tidak ditemukan");

        return Task.FromResult(ToResponse(maybe.Value));
    }

    private static DeliveryOrderGetResponse ToResponse(DeliveryOrderModel model)
    {
        var audit = new DeliveryOrderAuditResponse(
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId,
            model.AuditTrail.Voided.Timestamp);
        var items = model.ListItem.Select(x => new DeliveryOrderItemResponse(
            x.ItemNo,
            x.BrgId,
            x.LayananId,
            x.QtyOrder,
            x.QtyReceived,
            x.QtyRemaining,
            x.SatuanId,
            x.Harga,
            x.Diskon,
            x.Tax,
            x.TglEd,
            x.NoBatch,
            x.State)).ToList();

        return new DeliveryOrderGetResponse(
            model.DeliveryOrderId,
            model.DoNo,
            model.Supplier.SupplierId,
            model.Supplier.SupplierName,
            model.PoReffId,
            model.DoDate,
            model.State,
            model.Notes,
            audit,
            items);
    }
}

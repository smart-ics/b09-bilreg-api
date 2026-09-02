using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;

namespace Bilreg.Infrastructure.PurchaseContext.PurchaseOrderFeature;

public record PurchaseOrderItemDto(
    string PurchaseOrderId,
    decimal NoUrut,
    string BrgId,
    string SatuanId,
    decimal Harga,
    decimal QtyStok,
    decimal Qty,
    decimal Subtotal,
    decimal DiskonPercentage,
    decimal DiskonTotal,
    decimal TaxPercentage,
    decimal TaxTotal,
    decimal BiayaLain,
    decimal Total,
    string BrgName,
    string SatuanName)
{
    public static PurchaseOrderItemDto FromModel(string purchaseOrderId, PurchaseOrderItemType model)
    {
        var brg = model.Brg;
        var satuan = model.Satuan;
        var dto = new PurchaseOrderItemDto(purchaseOrderId, model.NoUrut, brg.BrgId, satuan.SatuanId, model.Harga, model.QtyStok, model.Qty,
            model.Subtotal, model.DiskonPercentage, model.DiskonTotal, model.TaxPercentage, model.TaxTotal, model.BiayaLain,
            model.Total, brg.BrgName, satuan.SatuanName);
        return dto;
    }

    public PurchaseOrderItemType ToModel()
    {
        var brg = new BrgReff(BrgId, BrgName);
        var satuan = new SatuanType(SatuanId, SatuanName);
        var model = new PurchaseOrderItemType((int)NoUrut, brg, satuan, Harga, QtyStok, Qty, Subtotal, DiskonPercentage, DiskonTotal,
            TaxPercentage, TaxTotal, BiayaLain, Total);
        return model;
    }
}
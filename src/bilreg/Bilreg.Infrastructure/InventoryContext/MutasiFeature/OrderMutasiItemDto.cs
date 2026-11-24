using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public record OrderMutasiItemDto(
    string OrderMutasiId,
    int NoUrut,
    string BrgId,
    string BrgName,
    decimal StokCurrent,
    decimal Qty,
    decimal QtyTerpenuhi,
    string SatuanId,
    string SatuanName)
{
    public static OrderMutasiItemDto FromModel(string orderMutasiId, OrderMutasiItemModel model)
    {
        var result = new OrderMutasiItemDto(
            orderMutasiId,
            model.NoUrut, 
            model.Brg.BrgId,
            model.Brg.BrgName,
            model.StokCurrent,
            model.Qty,
            model.QtyTerpenuhi,
            model.Satuan.SatuanId,
            model.Satuan.SatuanName);
        return result;
    }

    public OrderMutasiItemModel ToModel()
    {
        var brg = new BrgReff(BrgId, BrgName);
        var satuan = new SatuanType(SatuanId, SatuanName);
        var result = new OrderMutasiItemModel(NoUrut, brg, Qty, satuan, StokCurrent, QtyTerpenuhi);
        return result;
    }
}

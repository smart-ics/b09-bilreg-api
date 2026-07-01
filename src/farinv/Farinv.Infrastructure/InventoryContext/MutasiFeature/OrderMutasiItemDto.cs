using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.MutasiFeature;

namespace Farinv.Infrastructure.InventoryContext.MutasiFeature;

public record OrderMutasiItemDto(
    string OrderMutasiId,
    int NoUrut,
    string BrgId,
    string BrgName,
    decimal Qty,
    string SatuanId,
    string SatuanName)
{
    public static OrderMutasiItemDto FromModel(string orderMutasiId, OrderMutasiItemModel model)
    {
        var result = new OrderMutasiItemDto(orderMutasiId, model.NoUrut, 
            model.Brg.BrgId, model.Brg.BrgName, model.Qty, model.Satuan.SatuanId, model.Satuan.SatuanName );
        return result;
    }

    public OrderMutasiItemModel ToModel()
    {
        var brg = new BrgReff(BrgId, BrgName);
        var satuan = new SatuanType(SatuanId, SatuanName);
        var result = new OrderMutasiItemModel(NoUrut, brg, Qty, satuan);
        return result;
    }
}
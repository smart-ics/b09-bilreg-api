using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.Shared.Helpers;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public record StokBukuDto(
    string StokBukuId, string StokLayerId, int NoUrut,
    string BrgId, string LayananId,
    string PurchaseId, string ReceiveId, DateTime ExpDate, string BatchNo,
    int QtyIn, int QtyOut, decimal Hpp,
    string TrsReffId, DateTime TrsReffDate, string UseCase,
    DateTime EntryDate)
{
    public static StokBukuDto FromModel(StokModel header, StokLayerModel layer, StokBukuType model)
    {
        var result = new StokBukuDto(
            model.StokBukuId, layer.StokLayerId, model.NoUrut,
            header.BrgId, header.LayananId,
            layer.StokLot.PurchaseId,
            layer.StokLot.ReceiveId,
            layer.StokLot.ExpDate.ToDateTime(TimeOnly.MinValue),
            layer.StokLot.BatchNo,
            model.QtyIn, model.QtyOut, layer.Hpp,
            model.TrsReff.ReffId, model.TrsReff.ReffDate,
            model.UseCase, model.EntryDate);
        return result;
    }

    public (StokBukuType buku, string stokLayerId) ToModel()
    {
        var trsReff = new TrsReffType(TrsReffId, TrsReffDate);

        var buku = new StokBukuType(
            StokBukuId, NoUrut,  
            QtyIn, QtyOut, 
            trsReff, UseCase, EntryDate,
            ModelStateEnum.Unchange
        );
        return (buku, StokLayerId);
    }
}

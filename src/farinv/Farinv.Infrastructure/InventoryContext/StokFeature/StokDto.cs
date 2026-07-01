using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;

public record StokDto(
    string BrgId, string LayananId, 
    int Qty, string Satuan,
    string BrgName, string LayananName)
{
    public static StokDto FromModel(StokModel model)
    {
        var result = new StokDto(
            model.BrgId, model.LayananId, 
            model.Qty, model.Satuan,
            model.Brg.BrgName, model.Layanan.LayananName);
        return result;
    }

    public StokModel ToModel(
        IEnumerable<StokLayerModel> listLayer)
    {
        var result = new StokModel(
            BrgId,  LayananId, 
            new BrgReff(BrgId, BrgName), 
            new LayananReff(LayananId, LayananName), 
            Qty, Satuan, 
            listLayer);
        return result;
    }
}
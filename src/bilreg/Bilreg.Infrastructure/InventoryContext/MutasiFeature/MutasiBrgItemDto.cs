using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public record MutasiBrgItemDto(
    string MutasiBrgId,
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyKirim,
    string SatuanId,
    string SatuanName)
{
    public static MutasiBrgItemDto FromModel(string mutasiBrgId, MutasiBrgItemModel model)
    {
        return new MutasiBrgItemDto(
            mutasiBrgId,
            model.NoUrut,
            model.Brg.BrgId,
            model.Brg.BrgName,
            model.BatchNo,
            model.ExpiredDate,
            model.QtyKirim,
            model.Satuan.SatuanId,
            model.Satuan.SatuanName
        );
    }

    public MutasiBrgItemModel ToModel()
    {
        var brg = new BrgReff(BrgId, BrgName);
        var satuan = new SatuanType(SatuanId, SatuanName);
        return new MutasiBrgItemModel(NoUrut, brg, BatchNo, ExpiredDate, QtyKirim, satuan);
    }
}

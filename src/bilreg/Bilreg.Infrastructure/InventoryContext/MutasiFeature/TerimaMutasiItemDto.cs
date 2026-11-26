using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;

namespace Bilreg.Infrastructure.InventoryContext.MutasiFeature;

public record TerimaMutasiItemDto(
    string TerimaMutasiId,
    int NoUrut,
    string BrgId,
    string BrgName,
    string BatchNo,
    string ExpiredDate,
    decimal QtyKirim,
    decimal QtyTerimaBaik,
    decimal QtySelisih,
    string SatuanId,
    string SatuanName,
    string AlasanSelisih)
{
    public static TerimaMutasiItemDto FromModel(string terimaMutasiId, TerimaMutasiItemModel model)
    {
        return new TerimaMutasiItemDto(
            terimaMutasiId,
            model.NoUrut,
            model.Brg.BrgId,
            model.Brg.BrgName,
            model.BatchNo,
            model.ExpiredDate,
            model.QtyKirim,
            model.QtyTerimaBaik,
            model.QtySelisih,
            model.Satuan.SatuanId,
            model.Satuan.SatuanName,
            model.AlasanSelisih
        );
    }

    public TerimaMutasiItemModel ToModel()
    {
        var brg = new BrgReff(BrgId, BrgName);
        var satuan = new SatuanType(SatuanId, SatuanName);
        return TerimaMutasiItemModel.Load(
            NoUrut,
            brg,
            BatchNo,
            ExpiredDate,
            QtyKirim,
            QtyTerimaBaik,
            QtySelisih,
            satuan,
            AlasanSelisih);
    }
}

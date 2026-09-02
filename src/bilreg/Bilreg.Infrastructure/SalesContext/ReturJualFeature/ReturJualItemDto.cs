using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.ReturJualFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.SalesContext.ReturJualFeature;

public record ReturJualItemDto(
    string ReturJualId,
    string ReturJualItemId,
    int NoUrut,
    bool IsVoided,
    string BrgId,
    decimal QtyJual,
    decimal QtyRetur,
    string SatuanId,
    decimal HargaJual,
    decimal HargaRetur,
    decimal TaxPerUnit,
    decimal SubTotalJual,
    decimal SubTotalRetur,
    decimal SubTotalTax,
    decimal Total,
    string BrgName,
    string SatuanName)
{

    public static IEnumerable<ReturJualItemDto> FlattenFromModel(ReturJualModel model)
    {
        var list = new List<ReturJualItemDto>();
        var noUrut = 0;

        foreach (var item in model.ListItem)
        {
            noUrut++;
            list.Add(new ReturJualItemDto(
                model.ReturJualId,
                item.ReturJualItemId,
                noUrut,
                item.IsVoided,
                item.Brg.BrgId,
                item.QtyJual,
                item.QtyRetur,
                item.Satuan.SatuanId,
                item.Nilai.HargaJual,
                item.Nilai.HargaRetur,
                item.Nilai.TaxPerUnit,
                item.Nilai.SubTotalJual,
                item.Nilai.SubTotalRetur,
                item.Nilai.SubTotalTax,
                item.Nilai.Total,
                item.Brg.BrgName,
                item.Satuan.SatuanName
                ));
        }

        return list;
    }


    public ReturJualItemModel ToItemModel()
    {
        var brg = new BrgReff(BrgId, BrgName.Trim());
        var satuan = SatuanType.Create(
            string.IsNullOrWhiteSpace(SatuanId) ? AppConst.DASH : SatuanId,
            string.IsNullOrWhiteSpace(SatuanName) ? AppConst.DASH : SatuanName);
        var nilai = new NilaiItemReturType(HargaJual, HargaRetur, TaxPerUnit, SubTotalJual, SubTotalRetur, SubTotalTax, Total);

        return new ReturJualItemModel(ReturJualItemId, NoUrut, brg, satuan, QtyJual, QtyRetur, nilai, IsVoided); 
    }
}

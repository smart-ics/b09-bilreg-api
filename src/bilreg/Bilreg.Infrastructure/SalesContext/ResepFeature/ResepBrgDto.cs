using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.SalesContext.ResepFeature;

public record ResepBrgDto(
    string ResepId,
    decimal NoUrut,
    string BrgId,
    string BrgName,
    decimal Qty,
    string SatuanId,
    decimal Iter,
    bool IsRacik,
    bool IsKomponen,
    string RacikId,
    decimal Dosis,
    string DosisTxt,
    string Signa,
    string Etiket,
    decimal Frequency,
    decimal UnitDose,
    string SatuanName)
{
    public static IEnumerable<ResepBrgDto> FlattenFromModel(ResepModel model)
    {
        var list = new List<ResepBrgDto>();
        var noUrut = 0;

        foreach (var item in model.ListObat)
        {
            noUrut++;
            list.Add(new ResepBrgDto(
                model.ResepId,
                noUrut,
                item.Brg.BrgId,
                item.Brg.BrgName,
                item.Qty,
                item.Satuan.SatuanId,
                item.Iter,
                item.ListItemRacik.Any(),
                false,
                string.Empty,
                0,
                string.Empty,
                item.Etiket.Signa,
                item.Etiket.Instruction,
                item.Etiket.Frequency,
                item.Etiket.UnitDose,
                item.Satuan.SatuanName));

            foreach (var itemRacik in item.ListItemRacik)
            {
                noUrut++;
                list.Add(new ResepBrgDto(
                    model.ResepId,
                    noUrut,
                    itemRacik.Brg.BrgId,
                    $"   {itemRacik.Brg.BrgName}",
                    itemRacik.Qty,
                    itemRacik.Satuan.SatuanId,
                    0,
                    false,
                    true,
                    item.Brg.BrgId,
                    itemRacik.Dosis,
                    itemRacik.DosisTxt,
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    itemRacik.Satuan.SatuanName));
            }
        }

        return list;
    }

    public ResepObatType ToObatModel()
    {
        var brg = new BrgReff(BrgId, BrgName.Trim());
        var satuan = SatuanType.Create(SatuanId, SatuanName);
        var etiket = EtiketType.Load(Signa, Etiket, (int)Frequency, UnitDose, AppConst.DASH);
        return new ResepObatType((int)NoUrut, brg, satuan, Qty, (int)Iter, etiket);
    }
}

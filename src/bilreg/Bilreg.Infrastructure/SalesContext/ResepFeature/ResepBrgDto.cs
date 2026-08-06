using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.SalesContext.ResepFeature;

public record ResepBrgDto(
    string ResepId,
    int NoUrut,
    string BrgId,
    string BrgName,
    decimal Qty,
    string SatuanId,
    string SatuanName,
    int Iter,
    bool IsRacik,
    bool IsKomponen,
    string RacikId,
    decimal Dosis,
    string DosisTxt,
    string Signa,
    string Etiket,
    int Frequency,
    decimal UnitDose)
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
                item.Satuan.SatuanName,
                item.Iter,
                item.ListItemRacik.Any(),
                false,
                string.Empty,
                0,
                string.Empty,
                item.Etiket.Signa,
                item.Etiket.Instruction,
                item.Etiket.Frequency,
                item.Etiket.UnitDose));

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
                    itemRacik.Satuan.SatuanName,
                    0,
                    false,
                    true,
                    item.Brg.BrgId,
                    itemRacik.Dosis,
                    itemRacik.DosisTxt,
                    string.Empty,
                    string.Empty,
                    0,
                    0));
            }
        }

        return list;
    }

    public ResepObatType ToObatModel()
    {
        var brg = new BrgReff(BrgId, BrgName.Trim());
        var satuan = SatuanType.Create(SatuanId, SatuanName);
        var etiket = EtiketType.Load(Signa, Etiket, Frequency, UnitDose, AppConst.DASH);
        return new ResepObatType(NoUrut, brg, satuan, Qty, Iter, etiket);
    }
}

using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public record PenjualanItemDto(
    string PenjualanId,
    string PenjualanItemId,
    int NoUrut,
    bool IsVoided,
    string BrgId,
    string BrgName,
    string TipeBarangId,
    bool IsRacik,
    bool IsKomponen,
    string RacikId,
    decimal Dosis,
    string DosisTxt,
    decimal Qty,
    string SatuanId,
    decimal Harga,
    decimal Diskon,
    decimal Embalase,
    decimal SubTotal,
    decimal TaxProsen,
    decimal Tax,
    decimal Fee,
    decimal Total,
    decimal Bulat,
    decimal NilaiKlaim,
    string TipeJaminanId,
    string Etiket,
    int Frequency,
    decimal UnitDose,
    string Note,
    string SatuanName)
{
    public static IEnumerable<PenjualanItemDto> FlattenFromModel(PenjualanModel model)
    {
        var list = new List<PenjualanItemDto>();
        var noUrut = 0;

        foreach (var item in model.ListItem)
        {
            noUrut++;
            list.Add(new PenjualanItemDto(
                model.PenjualanId,
                item.PenjualanItemId,
                noUrut,
                item.IsVoided,
                item.Brg.BrgId,
                item.Brg.BrgName,
                model.TipeBrg.TipeBrgId,
                item.ListItemRacik.Any(),
                false,
                string.Empty,
                0,
                string.Empty,
                item.Qty,
                item.Satuan.SatuanId,
                item.Nilai.Harga,
                item.Nilai.Diskon,
                item.Nilai.Embalase,
                item.Nilai.SubTotal,
                item.Nilai.TaxProsen,
                item.Nilai.Tax,
                item.Nilai.Fee,
                item.Nilai.Total,
                item.Nilai.Bulat,
                item.Nilai.NilaiKlaim,
                model.TipeJaminan.TipeJaminanId,
                item.Etiket.Instruction,
                item.Etiket.Frequency,
                item.Etiket.UnitDose,
                item.Etiket.Note,
                item.Satuan.SatuanName));

            foreach (var racik in item.ListItemRacik)
            {
                noUrut++;
                var racikItemId = $"{model.PenjualanId}{noUrut:D3}";
                list.Add(new PenjualanItemDto(
                    model.PenjualanId,
                    racikItemId,
                    noUrut,
                    item.IsVoided,
                    racik.Brg.BrgId,
                    $"   {racik.Brg.BrgName}",
                    model.TipeBrg.TipeBrgId,
                    false,
                    true,
                    item.Brg.BrgId,
                    racik.Dosis,
                    racik.DosisTxt,
                    racik.Qty,
                    racik.Satuan.SatuanId,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    model.TipeJaminan.TipeJaminanId,
                    string.Empty,
                    0,
                    0,
                    AppConst.DASH, 
                    racik.Satuan.SatuanName));
            }
        }

        return list;
    }

    public PenjualanItemType ToItemModel()
    {
        var brg = new BrgReff(BrgId, BrgName.Trim());
        var satuan = SatuanType.Create(
            string.IsNullOrWhiteSpace(SatuanId) ? AppConst.DASH : SatuanId,
            string.IsNullOrWhiteSpace(SatuanName) ? AppConst.DASH : SatuanName);
        var etiket = EtiketType.Load(AppConst.DASH, Etiket, Frequency, UnitDose, Note);
        var nilai = new NilaiItemType(Harga, Diskon, Embalase, SubTotal, TaxProsen, Tax, Fee, Bulat, NilaiKlaim, Total);
        return new PenjualanItemType(PenjualanItemId, NoUrut, brg, satuan, Qty, etiket, nilai, IsVoided, []);
    }
}

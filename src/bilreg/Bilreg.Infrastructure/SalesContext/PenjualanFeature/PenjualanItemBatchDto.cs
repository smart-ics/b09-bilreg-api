using Bilreg.Domain.SalesContext.PenjualanFeature;

namespace Bilreg.Infrastructure.SalesContext.PenjualanFeature;

public record PenjualanItemBatchDto(
    string PenjualanId,
    string PenjualanItemId,
    string PenjualanItemBatchId,
    int NoUrut,
    bool IsVoided,
    string BrgId,
    decimal Qty,
    string SatuanId,
    string TglEd,
    string NoBatch,
    string PoId,
    string DoId,
    decimal Hpp)
{
    public static IEnumerable<PenjualanItemBatchDto> FlattenFromModel(PenjualanModel model)
    {
        var list = new List<PenjualanItemBatchDto>();
        var noUrut = 0;

        foreach (var item in model.ListItem)
        {
            foreach (var batch in item.ListBatch)
            {
                noUrut++;
                list.Add(new PenjualanItemBatchDto(
                    model.PenjualanId,
                    item.PenjualanItemId,
                    batch.PenjualanItemBatchId,
                    noUrut,
                    batch.IsVoided || item.IsVoided,
                    item.Brg.BrgId,
                    batch.Qty,
                    item.Satuan.SatuanId,
                    batch.TglEd.ToString("yyyy-MM-dd"),
                    batch.NoBatch,
                    batch.PoId,
                    batch.DoId,
                    batch.Hpp));
            }
        }

        return list;
    }

    public PenjualanItemBatchType ToBatchModel()
    {
        var tglEd = DateTime.TryParse(TglEd, out var ed) ? ed : new DateTime(3000, 1, 1);
        return new PenjualanItemBatchType(
            PenjualanItemBatchId,
            NoUrut,
            NoBatch,
            tglEd,
            PoId,
            DoId,
            Qty,
            Hpp,
            IsVoided);
    }
}

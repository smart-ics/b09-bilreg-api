using Bilreg.Domain.InventoryContext.PakaiBrgFeature;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

public class TindakanStokPendingDto
{
    public string PendingId { get; set; } = string.Empty;
    public string TindakanId { get; set; } = string.Empty;
    public string TindakanBarangId { get; set; } = string.Empty;
    public int NoUrut { get; set; }
    public string RegId { get; set; } = string.Empty;
    public string LayananId { get; set; } = string.Empty;
    public string BrgId { get; set; } = string.Empty;
    public string SatuanKecilId { get; set; } = string.Empty;
    public decimal QtyPending { get; set; }
    public decimal QtySettled { get; set; }
    public bool StatusPotStok { get; set; }
    public string PakaiBrgId { get; set; } = string.Empty;
    public bool IsVoid { get; set; }
    public DateTime SettledDate { get; set; } = new(3000, 1, 1);
    public DateTime VoidDate { get; set; } = new(3000, 1, 1);
    public string CrtUser { get; set; } = string.Empty;
    public DateTime CrtDate { get; set; } = new(3000, 1, 1);
    public string UpdUser { get; set; } = string.Empty;
    public DateTime UpdDate { get; set; } = new(3000, 1, 1);

    public TindakanStokPendingType ToModel() =>
        new(
            PendingId,
            TindakanId,
            TindakanBarangId,
            NoUrut,
            RegId,
            LayananId,
            BrgId,
            SatuanKecilId,
            QtyPending,
            QtySettled,
            StatusPotStok,
            PakaiBrgId,
            IsVoid,
            SettledDate,
            VoidDate,
            CrtDate
        );

    public static TindakanStokPendingDto FromModel(TindakanStokPendingType model, string userId = "")
    {
        var now = DateTime.Now;
        return new TindakanStokPendingDto
        {
            PendingId = model.PendingId,
            TindakanId = model.TindakanId,
            TindakanBarangId = model.TindakanBarangId,
            NoUrut = model.NoUrut,
            RegId = model.RegId,
            LayananId = model.LayananId,
            BrgId = model.BrgId,
            SatuanKecilId = model.SatuanKecilId,
            QtyPending = model.QtyPending,
            QtySettled = model.QtySettled,
            StatusPotStok = model.StatusPotStok,
            PakaiBrgId = model.PakaiBrgId,
            IsVoid = model.IsVoid,
            SettledDate = model.SettledDate == default ? new DateTime(3000, 1, 1) : model.SettledDate,
            VoidDate = model.VoidDate == default ? new DateTime(3000, 1, 1) : model.VoidDate,
            CrtUser = userId,
            CrtDate = model.CrtDate == default ? now : model.CrtDate,
            UpdUser = string.Empty,
            UpdDate = new DateTime(3000, 1, 1)
        };
    }
}

using Bilreg.Domain.ChargeContext.TindakanFeature;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class TindakanBarangDto
{
    public string TindakanBarangId { get; set; } = string.Empty;
    public string TindakanId { get; set; } = string.Empty;
    public int NoUrut { get; set; }
    public string BrgId { get; set; } = string.Empty;
    public string SatuanBomId { get; set; } = string.Empty;
    public decimal QtyBom { get; set; }
    public string SatuanKecilId { get; set; } = string.Empty;
    public decimal QtyKecilRequested { get; set; }
    public decimal QtyKecilDeducted { get; set; }
    public decimal QtyKecilPending { get; set; }
    public bool IsChargeBarang { get; set; }
    public decimal HargaJualTotal { get; set; }
    public string PakaiBrgId { get; set; } = string.Empty;
    public string TrsBillingId { get; set; } = string.Empty;
    public bool IsVoid { get; set; }
    public string BrgName { get; set; } = string.Empty;
    public decimal QtyPending { get; set; }
    public string CRTTGL { get; set; } = string.Empty;
    public string CRTJAM { get; set; } = string.Empty;
    public string CRTIPA { get; set; } = string.Empty;
    public string CRTVER { get; set; } = string.Empty;
    public string CRTUSR { get; set; } = string.Empty;
    public string UPDTGL { get; set; } = string.Empty;
    public string UPDJAM { get; set; } = string.Empty;
    public string UPDIPA { get; set; } = string.Empty;
    public string UPDVER { get; set; } = string.Empty;
    public string UPDUSR { get; set; } = string.Empty;

    public TindakanBarangType ToModel() =>
        new(
            TindakanBarangId,
            TindakanId,
            NoUrut,
            BrgId,
            SatuanBomId,
            QtyBom,
            SatuanKecilId,
            QtyKecilRequested,
            QtyKecilDeducted,
            QtyKecilPending,
            IsChargeBarang,
            HargaJualTotal,
            PakaiBrgId,
            TrsBillingId,
            IsVoid,
            BrgName,
            QtyPending > 0m || QtyKecilPending == 0m ? QtyPending : QtyKecilPending
        );

    public static TindakanBarangDto FromModel(TindakanBarangType model, string userId = "")
    {
        var now = DateTime.Now;
        return new TindakanBarangDto
        {
            TindakanBarangId = model.TindakanBarangId,
            TindakanId = model.TindakanId,
            NoUrut = model.NoUrut,
            BrgId = model.BrgId,
            SatuanBomId = model.SatuanBomId,
            QtyBom = model.QtyBom,
            SatuanKecilId = model.SatuanKecilId,
            QtyKecilRequested = model.QtyKecilRequested,
            QtyKecilDeducted = model.QtyKecilDeducted,
            QtyKecilPending = model.QtyKecilPending,
            IsChargeBarang = model.IsChargeBarang,
            HargaJualTotal = model.HargaJualTotal,
            PakaiBrgId = model.PakaiBrgId,
            TrsBillingId = model.TrsBillingId,
            IsVoid = model.IsVoid,
            CRTTGL = now.ToString("yyyy-MM-dd"),
            CRTJAM = now.ToString("HH:mm:ss"),
            CRTIPA = string.Empty,
            CRTVER = string.Empty,
            CRTUSR = userId,
            UPDTGL = "3000-01-01",
            UPDJAM = "00:00:00",
            UPDIPA = string.Empty,
            UPDVER = string.Empty,
            UPDUSR = string.Empty
        };
    }
}

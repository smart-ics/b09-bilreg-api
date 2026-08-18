using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.PakaiBedFeature;

public class PakaiBedModel : IPakaiBed
{
    public PakaiBedModel(string pakaiBedId, PeriodePakaiBedType periode,
        RegReff reg, LayananReff layanan, BedReff bed, TipeKamarReff tipeKamar,
        KelasReff kelas, decimal tarif, AuditTrailType auditTrail)
    {
        PakaiBedId = pakaiBedId;
        Periode = periode;
        Reg = reg;
        Layanan = layanan;
        Bed = bed;
        TipeKamar = tipeKamar;
        Kelas = kelas;
        Tarif = tarif;
        AuditTrail = auditTrail;
    }

    public static IPakaiBed Key(string id) => new PakaiBedModel(id, PeriodePakaiBedType.Default,
        RegModel.Default.ToReff(), LayananType.Default.ToReff(),
        BedType.Default.ToReff(), TipeKamarType.Default.ToReff(),
        KelasType.Default.ToReff(), 0, AuditTrailType.Default);

    public static PakaiBedModel Create(RegModel reg, AuditInfoType masuk,
        LayananType layanan, BedType bed, TipeKamarType tipeKamar,
        KelasType kelas, decimal tarif)
    {
        var newId = NunaId.New("PKB");
        var auditTrail = new AuditTrailType(masuk, AuditInfoType.Default, AuditInfoType.Default);
        return new PakaiBedModel(newId, 
            new PeriodePakaiBedType(masuk, AuditInfoType.Default),
            reg.ToReff(), layanan.ToReff(), bed.ToReff(), tipeKamar.ToReff(),
            kelas.ToReff(), tarif, auditTrail);
    }

public string PakaiBedId { get; init; }
    public PeriodePakaiBedType Periode { get; private set; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; private set; }
    public BedReff Bed { get; private set; }
    public TipeKamarReff TipeKamar { get; private set; }
    public KelasReff Kelas { get; private set; }
    public decimal Tarif { get; private set; }
    public AuditTrailType AuditTrail { get; init; }
    
    
}

public record PeriodePakaiBedType
{
    public PeriodePakaiBedType(AuditInfoType masuk, AuditInfoType keluar)
    {
        Masuk = masuk;
        Keluar = keluar;
    }
    public static PeriodePakaiBedType Default => new PeriodePakaiBedType(AuditInfoType.Default, AuditInfoType.Default);
    public AuditInfoType Masuk { get; init; }
    public AuditInfoType Keluar { get; init; }
}

public interface IPakaiBed
{
    string PakaiBedId { get; }
}




using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public record KarcisKomponenType 
{
    public KarcisKomponenType(KomponenReff komponenTarif, decimal nilai)
    {
        KomponenTarif = komponenTarif;
        Nilai = nilai;
    }
    public KomponenReff KomponenTarif { get; init; }
    public decimal Nilai { get; init; }
    
    public static KarcisKomponenType Default => new(KomponenType.Default.ToReff(), 0);
}
using Ardalis.GuardClauses;
using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;

namespace Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

public record KarcisKomponenModel 
{
    public KarcisKomponenModel(KomponenReff komponenTarif, decimal nilai)
    {
        Guard.Against.Null(komponenTarif, nameof(komponenTarif));
        Guard.Against.NegativeOrZero(nilai, nameof(nilai));
        
        KomponenTarif = komponenTarif;
        Nilai = nilai;
    }
    public KomponenReff KomponenTarif { get; init; }
    public decimal Nilai { get; init; }
    
    public static KarcisKomponenModel Default => new(KomponenType.Default.ToReff(), 0);
}
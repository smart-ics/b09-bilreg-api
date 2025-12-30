using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record TindakanKomponenType(
    KomponenReff Komponen,
    PpaReff Ppa,
    int NoUrut,
    decimal Nilai,
    decimal Qty,
    decimal SubTotal
)
{
    public static TindakanKomponenType Default => new(
        KomponenType.Default.ToReff(), 
        PpaType.Default.ToReff(), 
        0, 0, 0, 0);
    public static TindakanKomponenType Create(int noUrut, KomponenType komponen, PpaType ppa, decimal nilai)
    {
        if (!komponen.ListSatTugas.Any())
            return new TindakanKomponenType(
                komponen.ToReff(),
                ppa.ToReff(),
                noUrut,
                nilai,
                1,
                nilai);
        
        var ppaHasValidSatTugas = ppa.ListSatTugas
            .Select(x => x.SatTugas)
            .Any(ppaSatTugas => komponen.ListSatTugas
                .Any(kompSatTugas => ppaSatTugas.SatTugasId == kompSatTugas.SatTugasId));
        
        if (!ppaHasValidSatTugas)
            throw new InvalidOperationException(
                $"PPA '{ppa.PpaName}' ({ppa.PpaId}) tidak memiliki Satuan Tugas yang sesuai " +
                $"dengan komponen tarif '{komponen.KomponenName}' ({komponen.KomponenId})");

        return new TindakanKomponenType(
            komponen.ToReff(), 
            ppa.ToReff(), 
            noUrut, 
            nilai, 
            1, 
            nilai);
    }
}



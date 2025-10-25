using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public class BookingKunjunganModel
{
    public BookingKunjunganModel(LayananReff layanan, PetugasMedisReff dokter, 
        TimeOnly jamPraktek, int noAntrian)
    {
        Layanan = layanan;
        Dokter = dokter;
        JamPraktek = jamPraktek;
        NoAntrian = noAntrian;
    }

    public static BookingKunjunganModel Create(LayananReff layanan,
        PetugasMedisReff dokter, TimeOnly jamPraktek)
    {
        Guard.Against.Null(layanan, nameof(layanan));
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(jamPraktek, nameof(jamPraktek));
        
        var result = new BookingKunjunganModel(layanan, dokter, jamPraktek, -1);
        return result;
    }
    
    public LayananReff Layanan { get; init; } 
    public PetugasMedisReff Dokter { get; init; }
    public TimeOnly JamPraktek { get; init; }
    public int NoAntrian { get; private set; }

    public void AssignNoAntrian(int noAntrian) => NoAntrian = noAntrian;
}
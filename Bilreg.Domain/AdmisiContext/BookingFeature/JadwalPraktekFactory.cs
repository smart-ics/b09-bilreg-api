using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public interface IJadwalPraktekFactory : INunaFactory<JadwalPraktekType>
{
    JadwalPraktekType Create(PpaType dokter,
        LayananType layanan, DayOfWeek hari, TimeOnly jamMulai, TimeOnly jamSelesai, int maxPasien);
}
public class JadwalPraktekFactory : IJadwalPraktekFactory
{
    private readonly ISequencer _sequencer;

    public JadwalPraktekFactory(ISequencer sequencer)
    {
        _sequencer = sequencer;
    }

    public JadwalPraktekType Default =>
        new JadwalPraktekType("-", PpaType.Default.ToReff(), 
            LayananType.Default.ToReff(), LayananDkType.Default.ToReff(), GroupSpesialisType.Default,
            DayOfWeek.Monday, new TimeOnly(0, 0), new TimeOnly(0, 0), 0);
    
    public IJadwalPraktekKey Key(string id)
        => Default with { JadwalPraktekId = id };

    public JadwalPraktekType Create(PpaType dokter, LayananType layanan, DayOfWeek hari, TimeOnly jamMulai,
        TimeOnly jamSelesai, int maxPasien)
    {
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(dokter.Smf, nameof(dokter.Smf));
        
        var newNumber = _sequencer.GetNextNoUrut("BILRG_JadwalPraktek");
        var newId = $"JADW{newNumber:D3}";
        return new JadwalPraktekType(newId, dokter.ToReff(), layanan.ToReff(), 
            LayananDkType.Default.ToReff(), GroupSpesialisType.Default, 
            hari, jamMulai, jamSelesai, maxPasien);
    }
}
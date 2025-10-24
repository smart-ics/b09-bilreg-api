using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Domain.Helpers;
using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

namespace Bilreg.Domain.AdmisiContext.BookingFeature;

public interface IJadwalPraktekFactory : IFactory<JadwalPraktekType>
{
    JadwalPraktekType Create(PetugasMedisType dokter,
        LayananReff layanan, DayOfWeek hari, TimeOnly jamMulai, TimeOnly jamSelesai);
}
public class JadwalPraktekFactory : IJadwalPraktekFactory
{
    private readonly ISequencer _sequencer;

    public JadwalPraktekFactory(ISequencer sequencer)
    {
        _sequencer = sequencer;
    }

    public JadwalPraktekType Default =>
        new JadwalPraktekType("-", PetugasMedisType.Default.ToReff(), LayananType.Default.ToReff(), DayOfWeek.Monday, 
            new TimeOnly(0, 0), new TimeOnly(0, 0));
    
    public IJadwalPraktekKey Key(string id)
        => Default with { JadwalPraktekId = id };

    public JadwalPraktekType Create(PetugasMedisType dokter, LayananReff layanan, DayOfWeek hari, TimeOnly jamMulai,
        TimeOnly jamSelesai)
    {
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(dokter.Smf, nameof(dokter.Smf));
        
        var newNumber = _sequencer.GetNextNoUrut("BILRG_JadwalPraktek");
        var newId = $"JADW{newNumber:D3}";
        return new JadwalPraktekType(newId, dokter.ToReff(), layanan, hari, jamMulai, jamSelesai);
    }
}
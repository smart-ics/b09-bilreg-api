using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record JadwalPraktekDto(
    string JadwalPraktekId,
    string DokterId,
    string LayananId,
    int Hari,
    string JamMulai,
    string JamSelesai,
    string DokterName,
    string LayananName)
{
    public static JadwalPraktekDto FromModel(JadwalPraktekType model)
    {
        return new JadwalPraktekDto(model.JadwalPraktekId, model.Dokter.PetugasMedisId,
            model.Layanan.LayananId, (int)model.Hari, 
            model.JamMulai.ToString("HH:mm"), model.JamSelesai.ToString("HH:mm"),
            model.Dokter.PetugasMedisName, model.Layanan.LayananName);
    }
    public JadwalPraktekType ToModel()
    {
        var dokter = new PetugasMedisReff(DokterId, DokterName);
        var hari = (DayOfWeek)Hari;
        var layanan = new LayananReff(LayananId, LayananName);
        var jamMulai = TimeOnly.Parse(JamMulai);
        var jamSelesai = TimeOnly.Parse(JamSelesai);
        return new JadwalPraktekType(JadwalPraktekId, dokter, layanan, hari, jamMulai, jamSelesai);
    }
}
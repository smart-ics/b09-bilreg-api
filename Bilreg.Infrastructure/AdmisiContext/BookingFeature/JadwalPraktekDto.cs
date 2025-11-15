using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record JadwalPraktekDto(
    string JadwalPraktekId,
    string DokterId,
    string LayananId,
    int Hari,
    string JamMulai,
    string JamSelesai,
    string DokterName,
    string LayananName,
    string LayananDkId,
    string LayananDkName)
{
    public static JadwalPraktekDto FromModel(JadwalPraktekType model)
    {
        return new JadwalPraktekDto(model.JadwalPraktekId, model.Dokter.PetugasMedisId,
            model.Layanan.LayananId, (int)model.Hari, 
            model.JamMulai.ToString("HH:mm"), model.JamSelesai.ToString("HH:mm"),
            model.Dokter.PetugasMedisName, model.Layanan.LayananName,
            model.LayananDk.LayananDkId, model.LayananDk.LayananDkName);
    }
    public JadwalPraktekType ToModel()
    {
        var dokter = new PetugasMedisReff(DokterId, DokterName);
        var hari = (DayOfWeek)Hari;
        var layanan = new LayananReff(LayananId, LayananName);
        var lynDk = new LayananDkReff(LayananDkId, LayananDkName);
        var jamMulai = TimeOnly.Parse(JamMulai);
        var jamSelesai = TimeOnly.Parse(JamSelesai);
        return new JadwalPraktekType(JadwalPraktekId, dokter, layanan, lynDk, hari, jamMulai, jamSelesai);
    }
}
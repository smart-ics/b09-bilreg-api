using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record JadwalPraktekDto(
    string JadwalPraktekId,
    string DokterId,
    string LayananId,
    int Hari,
    string JamMulai,
    string JamSelesai,
    int MaxPasien,
    string DokterName,
    string LayananName,
    string LayananDkId,
    string LayananDkName,
    string GroupSpesialisId,
    string GroupSpesialisName)
{
    public static JadwalPraktekDto FromModel(JadwalPraktekType model)
    {
        return new JadwalPraktekDto(model.JadwalPraktekId, model.Dokter.PpaId,
            model.Layanan.LayananId, (int)model.Hari, 
            model.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture), 
            model.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture), 
            model.MaxPasien,
            model.Dokter.PpaName, model.Layanan.LayananName,
            model.LayananDk.LayananDkId, model.LayananDk.LayananDkName,
            model.GroupSpesialis.GroupSpesialisId, model.GroupSpesialis.GroupSpesialisName);
    }
    public JadwalPraktekType ToModel()
    {
        var dokter = new PpaReff(DokterId, DokterName);
        var hari = (DayOfWeek)Hari;
        var layanan = new LayananReff(LayananId, LayananName);
        var lynDk = new LayananDkReff(LayananDkId, LayananDkName);
        var groupSpesialis = new GroupSpesialisType(GroupSpesialisId, GroupSpesialisName);
        var jamMulai = TimeOnly.ParseExact(JamMulai, "HH:mm", CultureInfo.InvariantCulture);
        var jamSelesai = TimeOnly.ParseExact(JamSelesai, "HH:mm", CultureInfo.InvariantCulture);
        return new JadwalPraktekType(JadwalPraktekId, dokter, layanan, 
            lynDk, groupSpesialis, hari, jamMulai, jamSelesai, MaxPasien);
    }
}
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using System.Globalization;
using System.Text.Json;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public record JadwalPraktekDto(
    string JadwalPraktekId,
    string DokterId,
    string LayananId,
    string RuangId,
    int Hari,
    string JamMulai,
    string JamSelesai,
    int MaxPasien,
    string AntrianPattern,
    string DokterName,
    string LayananName,
    string LayananDkId,
    string LayananDkName,
    string GroupSpesialisId,
    string GroupSpesialisName, 
    string RuangName,
    string PrefixAntrian)
{
    public static JadwalPraktekDto FromModel(JadwalPraktekType model)
    {
        
        var antrianPatternStr = JsonSerializer.Serialize(model.AntrianPattern);
        
        return new JadwalPraktekDto(model.JadwalPraktekId, model.Dokter.PpaId,
            model.Layanan.LayananId, model.Ruang.RuangId, (int)model.Hari, 
            model.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture), 
            model.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture), 
            model.MaxPasien, antrianPatternStr, 
            model.Dokter.PpaName, model.Layanan.LayananName, model.LayananDk.LayananDkId,
            model.LayananDk.LayananDkName, model.GroupSpesialis.GroupSpesialisId,
            model.GroupSpesialis.GroupSpesialisName, model.Ruang.RuangName, model.Ruang.PrefixAntrian);
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
        var ruang = new RuangType(RuangId, RuangName, PrefixAntrian);
        var pattern = JsonSerializer.Deserialize<AntrianPatternType>(AntrianPattern)
            ?? AntrianPatternType.Default;
        return new JadwalPraktekType(JadwalPraktekId, dokter, layanan, 
            lynDk, groupSpesialis, ruang, hari, jamMulai, jamSelesai, MaxPasien, pattern);
    }
}
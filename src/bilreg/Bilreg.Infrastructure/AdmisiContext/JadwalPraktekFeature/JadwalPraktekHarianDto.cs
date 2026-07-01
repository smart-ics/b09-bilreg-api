using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Infrastructure.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

public record JadwalPraktekHarianDto(
    string JadwalPraktekHarianId,
    string? JadwalPraktekId,
    DateTime TglPraktek,
    string DokterId,
    string LayananId,
    string RuangId,
    string JamMulai,
    string JamSelesai,
    int MaxPasien,
    string AntrianPattern,
    string Status,
    string Source,
    string? Catatan,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string DokterName,
    string LayananName,
    string LayananDkId,
    string LayananDkName,
    string GroupSpesialisId,
    string GroupSpesialisName,
    string RuangName,
    string PrefixAntrian)
{
    public static JadwalPraktekHarianDto FromModel(JadwalPraktekHarianType model)
    {
        var pattern = AntrianPatternFactory.ToStringJson(model.AntrianPattern);
        return new JadwalPraktekHarianDto(
            model.JadwalPraktekHarianId,
            model.JadwalPraktekId,
            model.TglPraktek.ToDateTime(TimeOnly.MinValue),
            model.Dokter.PpaId,
            model.Layanan.LayananId,
            model.Ruang.RuangId,
            model.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
            model.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture),
            model.MaxPasien,
            pattern,
            model.Status.ToString(),
            model.Source.ToString(),
            model.Catatan,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.Dokter.PpaName,
            model.Layanan.LayananName,
            model.LayananDk.LayananDkId,
            model.LayananDk.LayananDkName,
            model.GroupSpesialis.GroupSpesialisId,
            model.GroupSpesialis.GroupSpesialisName,
            model.Ruang.RuangName,
            model.Ruang.PrefixAntrian);
    }

    public JadwalPraktekHarianType ToModel()
    {
        var dokter = new PpaReff(DokterId, DokterName);
        var layanan = new LayananReff(LayananId, LayananName);
        var lynDk = new LayananDkReff(LayananDkId, LayananDkName);
        var groupSpesialis = new GroupSpesialisType(GroupSpesialisId, GroupSpesialisName);
        var ruang = new RuangType(RuangId, RuangName, PrefixAntrian);
        var jamMulai = TimeOnly.ParseExact(JamMulai, "HH:mm", CultureInfo.InvariantCulture);
        var jamSelesai = TimeOnly.ParseExact(JamSelesai, "HH:mm", CultureInfo.InvariantCulture);
        var pattern = AntrianPatternFactory.FromStringJson(AntrianPattern);
        var audit = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            AuditInfoType.Default);

        return new JadwalPraktekHarianType(
            JadwalPraktekHarianId, JadwalPraktekId,
            DateOnly.FromDateTime(TglPraktek),
            dokter, layanan, lynDk, groupSpesialis, ruang,
            jamMulai, jamSelesai, MaxPasien, pattern,
            Enum.Parse<JadwalPraktekScheduleStatus>(Status),
            Enum.Parse<JadwalPraktekHarianSource>(Source),
            Catatan, audit);
    }
}

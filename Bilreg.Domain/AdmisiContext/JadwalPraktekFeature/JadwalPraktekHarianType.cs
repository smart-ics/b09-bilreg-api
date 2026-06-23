using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;

public record JadwalPraktekHarianType : IJadwalPraktekHarianKey
{
    private const string IdPrefix = "JPH";

    public JadwalPraktekHarianType(
        string jadwalPraktekHarianId,
        string? jadwalPraktekId,
        DateOnly tglPraktek,
        PpaReff dokter,
        LayananReff layanan,
        LayananDkReff layananDk,
        GroupSpesialisType groupSpesialis,
        RuangType ruang,
        TimeOnly jamMulai,
        TimeOnly jamSelesai,
        int maxPasien,
        AntrianPatternType antrianPattern,
        JadwalPraktekScheduleStatus status,
        JadwalPraktekHarianSource source,
        string? catatan,
        AuditTrailType auditTrail)
    {
        JadwalPraktekHarianId = jadwalPraktekHarianId;
        JadwalPraktekId = jadwalPraktekId;
        TglPraktek = tglPraktek;
        Dokter = dokter;
        Layanan = layanan;
        LayananDk = layananDk;
        GroupSpesialis = groupSpesialis;
        Ruang = ruang;
        JamMulai = jamMulai;
        JamSelesai = jamSelesai;
        MaxPasien = maxPasien;
        AntrianPattern = antrianPattern;
        Status = status;
        Source = source;
        Catatan = catatan;
        AuditTrail = auditTrail;
    }

    public static JadwalPraktekHarianType Default => new(
        "-", null, DateOnly.MinValue,
        PpaType.Default.ToReff(), LayananType.Default.ToReff(), LayananDkType.Default.ToReff(),
        GroupSpesialisType.Default, RuangType.Default,
        TimeOnly.MinValue, TimeOnly.MinValue, 0, AntrianPatternType.Default,
        JadwalPraktekScheduleStatus.ACTIVE, JadwalPraktekHarianSource.MANUAL, null,
        AuditTrailType.Default);

    public static IJadwalPraktekHarianKey Key(string id) => Default with { JadwalPraktekHarianId = id };

    public static JadwalPraktekHarianType Create(
        string? jadwalPraktekId,
        DateOnly tglPraktek,
        PpaType dokter,
        LayananType layanan,
        RuangType ruang,
        TimeOnly jamMulai,
        TimeOnly jamSelesai,
        int maxPasien,
        AntrianPatternType antrianPattern,
        JadwalPraktekHarianSource source,
        string? catatan,
        string userId)
    {
        Guard.Against.Null(dokter);
        Guard.Against.Null(layanan);
        Guard.Against.Null(ruang);

        var newId = NunaId.New(IdPrefix);
        var audit = AuditTrailType.Create(userId, DateTime.Now);

        return new JadwalPraktekHarianType(
            newId, jadwalPraktekId, tglPraktek,
            dokter.ToReff(), layanan.ToReff(), layanan.LayananDk,
            dokter.GroupSpesialis, ruang,
            jamMulai, jamSelesai, maxPasien, antrianPattern,
            JadwalPraktekScheduleStatus.ACTIVE, source, catatan, audit);
    }

    public string JadwalPraktekHarianId { get; init; }
    public string? JadwalPraktekId { get; init; }
    public DateOnly TglPraktek { get; init; }
    public PpaReff Dokter { get; init; }
    public LayananReff Layanan { get; init; }
    public LayananDkReff LayananDk { get; init; }
    public GroupSpesialisType GroupSpesialis { get; init; }
    public RuangType Ruang { get; init; }
    public TimeOnly JamMulai { get; init; }
    public TimeOnly JamSelesai { get; init; }
    public int MaxPasien { get; init; }
    public AntrianPatternType AntrianPattern { get; init; }
    public JadwalPraktekScheduleStatus Status { get; init; }
    public JadwalPraktekHarianSource Source { get; init; }
    public string? Catatan { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    public JadwalPraktekHarianType Cancel(string catatan, string userId)
    {
        if (Status == JadwalPraktekScheduleStatus.CANCELLED)
            throw new ArgumentException("Jadwal harian sudah dibatalkan");

        var audit = new AuditTrailType(
            AuditTrail.Created,
            new AuditInfoType(userId, DateTime.Now),
            AuditTrail.Voided);

        return this with
        {
            Status = JadwalPraktekScheduleStatus.CANCELLED,
            Catatan = catatan,
            AuditTrail = audit
        };
    }

    public JadwalPraktekHarianType ApplyManualOverride(
        PpaReff dokter, LayananReff layanan, RuangType ruang,
        TimeOnly jamMulai, TimeOnly jamSelesai, int maxPasien,
        AntrianPatternType antrianPattern, string? catatan, string userId)
    {
        var audit = new AuditTrailType(
            AuditTrail.Created,
            new AuditInfoType(userId, DateTime.Now),
            AuditTrail.Voided);

        return this with
        {
            Dokter = dokter,
            Layanan = layanan,
            Ruang = ruang,
            JamMulai = jamMulai,
            JamSelesai = jamSelesai,
            MaxPasien = maxPasien,
            AntrianPattern = antrianPattern,
            Source = JadwalPraktekHarianSource.MANUAL,
            Catatan = catatan,
            Status = JadwalPraktekScheduleStatus.ACTIVE,
            AuditTrail = audit
        };
    }
}

public interface IJadwalPraktekHarianKey
{
    string JadwalPraktekHarianId { get; }
}

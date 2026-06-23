using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;

/// <summary>
/// Runtime projection — never persisted. Materialized by <see cref="IJadwalPraktekResolver"/>.
/// </summary>
public record JadwalPraktekEffective
{
    public string? JadwalPraktekId { get; init; }
    public string? JadwalPraktekHarianId { get; init; }
    public DateOnly TglPraktek { get; init; }
    public PpaReff Dokter { get; init; } = PpaType.Default.ToReff();
    public LayananReff Layanan { get; init; } = LayananType.Default.ToReff();
    public LayananDkReff LayananDk { get; init; } = LayananDkType.Default.ToReff();
    public GroupSpesialisType GroupSpesialis { get; init; } = GroupSpesialisType.Default;
    public RuangType Ruang { get; init; } = RuangType.Default;
    public TimeOnly JamMulai { get; init; }
    public TimeOnly JamSelesai { get; init; }
    public int MaxPasien { get; init; }
    public AntrianPatternType AntrianPattern { get; init; } = AntrianPatternType.Default;
    public JadwalPraktekScheduleStatus Status { get; init; }
    public JadwalPraktekSource Source { get; init; }
}

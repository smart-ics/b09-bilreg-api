using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public record JadwalPraktekResolveOptions(
    bool AllowSynthetic = false,
    bool ThrowIfCancelled = true,
    bool ThrowIfNotFound = true);

public record JadwalPraktekResolveRequest(
    DateOnly TglPraktek,
    IPpaKey Dokter,
    TimeOnly? JamMulai,
    JadwalPraktekResolveOptions Options);

public record JadwalPraktekResolveForDateRequest(
    DateOnly TglPraktek,
    bool IncludeCancelled = false,
    IPpaKey? DokterFilter = null);

public interface IJadwalPraktekResolver
{
    JadwalPraktekEffective Resolve(JadwalPraktekResolveRequest request);
    IEnumerable<JadwalPraktekEffective> ResolveForDate(JadwalPraktekResolveForDateRequest request);
}

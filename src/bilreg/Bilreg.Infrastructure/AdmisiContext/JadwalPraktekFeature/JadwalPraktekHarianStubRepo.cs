using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;

/// <summary>
/// No-op repository until <c>BILRG_JadwalPraktekHarian</c> table is deployed (M2).
/// Replaced by <see cref="JadwalPraktekHarianRepo"/> after migration.
/// </summary>
public class JadwalPraktekHarianStubRepo : IJadwalPraktekHarianRepo
{
    public void SaveChanges(JadwalPraktekHarianType entity) { }

    public MayBe<JadwalPraktekHarianType> LoadEntity(IJadwalPraktekHarianKey key)
        => MayBe<JadwalPraktekHarianType>.None;

    public IEnumerable<JadwalPraktekHarianType> ListByDate(DateOnly tglPraktek)
        => [];

    public IEnumerable<JadwalPraktekHarianType> ListByDateAndDokter(DateOnly tglPraktek, IPpaKey dokter)
        => [];
}

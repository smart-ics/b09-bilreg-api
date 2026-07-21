using Bilreg.Domain.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Result of a single queue-number reservation. Carries legacy map mutations ready for persistence.
/// Does not include canonical Waiting/In Service/Done state.
/// </summary>
public sealed record ReservedQueueNumber(
    int NoUrut,
    AntrianMapModel Map,
    AntrianMapDetilModel MapDetil)
{
    public static ReservedQueueNumber FromMap(AntrianMapModel map, AntrianMapDetilModel detil) =>
        new(detil.NoUrut, map, detil);
}

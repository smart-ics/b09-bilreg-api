using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedAlokasiRepo :
    ILoadEntity<PakaiBedAlokasiModel, IPakaiBedAlokasiKey>
{
    void SaveChanges(PakaiBedAlokasiModel alokasi, PakaiBedModel legacyPakaiBed);
}

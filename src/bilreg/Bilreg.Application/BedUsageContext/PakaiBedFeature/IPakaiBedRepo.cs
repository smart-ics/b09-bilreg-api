using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedRepo :
    ISaveChange<PakaiBedModel>,
    ILoadEntity<PakaiBedModel, IPakaiBed>,
    IDeleteEntity<IPakaiBed>,
    IListData<PakaiBedModel, IRegKey>
{
    MayBe<PakaiBedModel> GetActiveOccupant(IBedKey bedKey);
}

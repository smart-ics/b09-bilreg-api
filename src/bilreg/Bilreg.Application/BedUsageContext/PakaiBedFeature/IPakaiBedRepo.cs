using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedRepo :
    ISaveChange<PakaiBedModel>,
    ILoadEntity<PakaiBedModel, IPakaiBed>,
    IDeleteEntity<IPakaiBed>,
    IListData<PakaiBedModel, IRegKey>
{
}

using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IProsedureMasukInapRepo :
    ILoadEntity<ProsedurMasukInapType, IProsedurMasukInapKey>,
    IListData<ProsedurMasukInapType>
{
}

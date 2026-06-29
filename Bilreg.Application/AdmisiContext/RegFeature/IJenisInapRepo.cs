using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IJenisInapRepo :
    ILoadEntity<JenisInapType, IJenisInapKey>,
    IListData<JenisInapType>
{
}

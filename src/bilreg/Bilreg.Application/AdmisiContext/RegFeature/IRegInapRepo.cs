using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IRegInapRepo :
    ISaveChange<RegInapModel>,
    ILoadEntity<RegInapModel, IRegKey>,
    IDeleteEntity<IRegKey>
{
}

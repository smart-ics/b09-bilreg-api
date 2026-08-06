using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.SalesContext.ResepFeature;

public interface IResepRepo :
    ISaveChange<ResepModel>,
    ILoadEntity<ResepModel, IResepKey>,
    IDeleteEntity<IResepKey>,
    IListData<ResepModel, IRegKey>
{
}

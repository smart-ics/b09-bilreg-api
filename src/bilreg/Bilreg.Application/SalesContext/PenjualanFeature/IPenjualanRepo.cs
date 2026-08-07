using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.Shared;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.SalesContext.PenjualanFeature;

public interface IPenjualanRepo :
    ISaveChange<PenjualanModel>,
    ILoadEntity<PenjualanModel, IPenjualanKey>,
    IDeleteEntity<IPenjualanKey>,
    IListData<PenjualanModel, IRegKey>
{
}

using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.SalesContext.PenjualanFeature;

public interface IPenjualanRepo :
    ISaveChange<PenjualanModel>,
    ILoadEntity<PenjualanModel, IPenjualanKey>,
    IDeleteEntity<IPenjualanKey>,
    IListData<PenjualanModel, IRegKey>
{
}

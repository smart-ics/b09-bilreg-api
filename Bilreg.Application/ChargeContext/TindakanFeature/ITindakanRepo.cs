using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface ITindakanRepo :
    ISaveChange<TindakanModel>,
    ILoadEntity<TindakanModel, ITindakanKey>,
    IDelete<ITindakanKey>,
    IListData<TindakanView, IRegKey>
{
}
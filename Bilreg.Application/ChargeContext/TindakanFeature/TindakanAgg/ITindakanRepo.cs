using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public interface ITindakanRepo :
    ISaveChange<TindakanModel>,
    ILoadEntity<TindakanModel, ITindakanKey>,
    IDelete<ITindakanKey>,
    IListData<TindakanView, IRegKey>
{
}
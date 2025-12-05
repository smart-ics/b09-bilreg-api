using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public interface IOrderTindakanRepo :
    ISaveChange<OrderTindakanModel>,
    ILoadEntity<OrderTindakanModel, IOrderTindakanKey>,
    IDelete<IOrderTindakanKey>,
    IListData<OrderTindakanModel, ILayananKey>
{
}
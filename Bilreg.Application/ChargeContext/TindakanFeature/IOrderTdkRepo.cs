using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public interface IOrderTdkRepo :
    ISaveChange<OrderTdkModel>,
    ILoadEntity<OrderTdkModel, IOrderTdkKey>,
    IDelete<IOrderTdkKey>,
    IListData<OrderTdkModel, IPasienKey>
{
}
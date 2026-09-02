using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.DepositFeature;

public interface IDepositRepo :
    ISaveChange<DepositModel>,
    ILoadEntity<DepositModel, IDepositId>,
    IListData<DepositModel, IRegKey>
{
}

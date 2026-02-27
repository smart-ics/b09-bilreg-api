using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;

public interface IRegHutangRepo :
    IListData<RegHutangType, IPasienKey>
{
}

using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;

public interface IRegBiayaRepo :
    IListData<RegBiayaType, IRegKey>
{
}

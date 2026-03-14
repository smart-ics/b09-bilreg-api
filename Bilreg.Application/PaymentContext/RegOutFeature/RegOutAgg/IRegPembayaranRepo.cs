using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;

public interface IRegPembayaranRepo :
    IListData<RegPembayaranType, IRegKey>
{
}

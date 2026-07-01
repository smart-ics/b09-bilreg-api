using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;

public interface IRegPembayaranRepo :
    IInsert<RegPembayaranType>,
    IUpdate<RegPembayaranType>,
    IListData<RegPembayaranType, IRegKey>
{
}

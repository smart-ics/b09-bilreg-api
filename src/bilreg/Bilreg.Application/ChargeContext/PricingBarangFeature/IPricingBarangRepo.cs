using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

public interface IPricingBarangRepo :
    ISaveChange<PricingBarangModel>,
    ILoadEntity<PricingBarangModel, IPricingBarangKey>,
    IDeleteEntity<IPricingBarangKey>,
    IListData<PricingBarangModel>
{
}

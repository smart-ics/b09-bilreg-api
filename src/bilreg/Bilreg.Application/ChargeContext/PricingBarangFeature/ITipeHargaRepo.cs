using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature;

public interface ITipeHargaRepo :
    ISaveChange<TipeHargaModel>,
    ILoadEntity<TipeHargaModel, ITipeHargaKey>,
    IDeleteEntity<ITipeHargaKey>,
    IListData<TipeHargaModel>
{
}

using Bilreg.Domain.InventoryContext.MutasiFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface IOrderMutasiRepo :
    ISaveChange<OrderMutasiModel>,
    ILoadEntity<OrderMutasiModel, IOrderMutasiKey>,
    IDeleteEntity<IOrderMutasiKey>,
    IListData<OrderMutasiHeaderView, Periode>
{
    IEnumerable<OrderMutasiHeaderView> ListDraftState();
    IEnumerable<OrderMutasiHeaderView> ListApprovedState(Periode filter);
}

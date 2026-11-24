using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface IOrderMutasiRepo :
    ISaveChange<OrderMutasiModel>,
    ILoadEntity<OrderMutasiModel, IOrderMutasiKey>,
    IDeleteEntity<IOrderMutasiKey>,
    IListData<OrderMutasiHeaderView, Periode>,
    IListData<OrderMutasiHeaderView, ILayananKey, Periode>
{
    IEnumerable<OrderMutasiHeaderView> ListDraftState();
    IEnumerable<OrderMutasiHeaderView> ListApprovedState(Periode filter);
    IEnumerable<OrderMutasiHeaderView> ListByLayananOrder(ILayananKey layananOrder, Periode filter) => ListData(layananOrder, filter);
}

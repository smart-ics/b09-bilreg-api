using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.InventoryContext.StokFeature;

public interface IStokRepo :
    ISaveChange<StokModel>,
    ILoadEntity<StokModel, IStokKey>,
    IDeleteEntity<IStokKey>,
    IListData<StokBalanceView>
{
}

public record StokBalanceView(BrgReff Brg, LayananReff Layanan, int Qty, string Satuan);
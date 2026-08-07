using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StokFeature;

public interface IStokRepo :
    ISaveChange<StokModel>,
    ILoadEntity<StokModel, IStokKey>,
    IDeleteEntity<IStokKey>,
    IListData<StokBalanceView>
{
}

public record StokBalanceView(BrgReff Brg, LayananReff Layanan, int Qty, string Satuan);
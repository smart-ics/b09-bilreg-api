using Bilreg.Domain.InventoryContext.StokFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StokFeature;

public interface ILayananRepo :
    ILoadEntity<LayananType, ILayananKey>,
    IListData<LayananType>
{
}
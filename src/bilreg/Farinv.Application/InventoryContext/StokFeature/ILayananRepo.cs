using Farinv.Domain.InventoryContext.StokFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.InventoryContext.StokFeature;

public interface ILayananRepo :
    ILoadEntity<LayananType, ILayananKey>,
    IListData<LayananType>
{
}
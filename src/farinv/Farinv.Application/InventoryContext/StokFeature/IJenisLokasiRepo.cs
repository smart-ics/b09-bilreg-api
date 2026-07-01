using Farinv.Domain.InventoryContext.StokFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.InventoryContext.StokFeature;

public interface IJenisLokasiRepo :
    ISaveChange<JenisLokasiType>,
    ILoadEntity<JenisLokasiType, IJenisLokasiKey>,
    IDeleteEntity<IJenisLokasiKey>,
    IListData<JenisLokasiType>
{
}
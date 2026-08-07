using Bilreg.Domain.InventoryContext.StokFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.InventoryContext.StokFeature;

public interface IJenisLokasiRepo :
    ISaveChange<JenisLokasiType>,
    ILoadEntity<JenisLokasiType, IJenisLokasiKey>,
    IDeleteEntity<IJenisLokasiKey>,
    IListData<JenisLokasiType>
{
}
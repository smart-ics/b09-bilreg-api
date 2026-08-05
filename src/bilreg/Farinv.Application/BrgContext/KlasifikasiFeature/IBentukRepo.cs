using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.KlasifikasiFeature;

public interface IBentukRepo :
    ISaveChange<BentukType>,
    ILoadEntity<BentukType, IBentukKey>,
    IDeleteEntity<IBentukKey>,
    IListData<BentukType>
{
}

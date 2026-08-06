using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface IBentukRepo :
    ISaveChange<BentukType>,
    ILoadEntity<BentukType, IBentukKey>,
    IDeleteEntity<IBentukKey>,
    IListData<BentukType>
{
}

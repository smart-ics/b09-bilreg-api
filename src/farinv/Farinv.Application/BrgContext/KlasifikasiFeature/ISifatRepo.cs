using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.KlasifikasiFeature;

public interface ISifatRepo :
    ISaveChange<SifatType>,
    ILoadEntity<SifatType, ISifatKey>,
    IDeleteEntity<ISifatKey>,
    IListData<SifatType>
{
}

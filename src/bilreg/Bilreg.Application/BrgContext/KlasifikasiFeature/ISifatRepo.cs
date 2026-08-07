using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface ISifatRepo :
    ISaveChange<SifatType>,
    ILoadEntity<SifatType, ISifatKey>,
    IDeleteEntity<ISifatKey>,
    IListData<SifatType>
{
}

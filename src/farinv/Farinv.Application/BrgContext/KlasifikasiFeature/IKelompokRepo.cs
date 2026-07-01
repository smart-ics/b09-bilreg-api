using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.KlasifikasiFeature;

public interface IKelompokRepo :
    ISaveChange<KelompokType>,
    ILoadEntity<KelompokType, IKelompokKey>,
    IDeleteEntity<IKelompokKey>,
    IListData<KelompokType>
{
}
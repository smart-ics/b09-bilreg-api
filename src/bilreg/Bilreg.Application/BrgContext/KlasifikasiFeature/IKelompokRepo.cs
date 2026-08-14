using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface IKelompokRepo :
    ISaveChange<KelompokType>,
    ILoadEntity<KelompokType, IKelompokKey>,
    IDeleteEntity<IKelompokKey>,
    IListData<KelompokType>
{
}
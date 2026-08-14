using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface IPabrikRepo :
    ISaveChange<PabrikType>,
    ILoadEntity<PabrikType, IPabrikKey>,
    IDeleteEntity<IPabrikKey>,
    IListData<PabrikType>
{
}

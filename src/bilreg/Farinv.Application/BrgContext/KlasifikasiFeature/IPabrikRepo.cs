using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.KlasifikasiFeature;

public interface IPabrikRepo :
    ISaveChange<PabrikType>,
    ILoadEntity<PabrikType, IPabrikKey>,
    IDeleteEntity<IPabrikKey>,
    IListData<PabrikType>
{
}

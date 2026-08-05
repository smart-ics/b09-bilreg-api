using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.KlasifikasiFeature;

public interface IGenerikRepo :
    ISaveChange<GenerikType>,
    ILoadEntity<GenerikType, IGenerikKey>,
    IDeleteEntity<IGenerikKey>,
    IListData<GenerikType>
{
}
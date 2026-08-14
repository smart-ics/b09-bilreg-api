using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface IGenerikRepo :
    ISaveChange<GenerikType>,
    ILoadEntity<GenerikType, IGenerikKey>,
    IDeleteEntity<IGenerikKey>,
    IListData<GenerikType>
{
}
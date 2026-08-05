using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.KlasifikasiFeature;

public interface IGroupObatDkRepo :
    ISaveChange<GroupObatDkType>,
    ILoadEntity<GroupObatDkType, IGroupObatDkKey>,
    IDeleteEntity<IGroupObatDkKey>,
    IListData<GroupObatDkType>
{
}
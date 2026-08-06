using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.KlasifikasiFeature;

public interface IGroupObatDkRepo :
    ISaveChange<GroupObatDkType>,
    ILoadEntity<GroupObatDkType, IGroupObatDkKey>,
    IDeleteEntity<IGroupObatDkKey>,
    IListData<GroupObatDkType>
{
}
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;
//
// public interface IAntrianDal :
//     IInsert<AntrianModel>,
//     IUpdate<AntrianModel>,
//     IDelete<IAntrianKey>,
//     IGetDataMayBe<AntrianModel, IAntrianKey>,
//     IListData<AntrianModel, DateOnly, IServicePointKey>
// {
// }

public interface IAntrianRepo :
    ISaveChange<AntrianModel>,
    ILoadEntity<AntrianModel, IAntrianKey>,
    IListData<IAntrianHeaderView, DateOnly>
{
}
using Bilreg.Domain.AdmisiContext.JaminanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public interface IGroupJaminanDal :
    IInsert<GroupJaminanType>,
    IUpdate<GroupJaminanType>,
    IDelete<IGroupJaminanKey>,
    IGetDataMayBe<GroupJaminanType, IGroupJaminanKey>,
    IListDataMayBe<GroupJaminanType>
{
}
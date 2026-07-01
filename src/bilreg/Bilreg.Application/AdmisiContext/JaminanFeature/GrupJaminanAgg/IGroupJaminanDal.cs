using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.GrupJaminanAgg;

public interface IGroupJaminanDal :
    IInsert<GroupJaminanType>,
    IUpdate<GroupJaminanType>,
    IDelete<IGroupJaminanKey>,
    IGetDataMayBe<GroupJaminanType, IGroupJaminanKey>,
    IListDataMayBe<GroupJaminanType>
{
}
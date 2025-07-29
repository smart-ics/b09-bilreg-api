using Bilreg.Domain.AdmisiContext.JaminanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public interface IJaminanDal :
    IInsert<JaminanType>,
    IUpdate<JaminanType>,
    IDelete<IJaminanKey>,
    IGetDataMayBe<JaminanType, IJaminanKey>,
    IListDataMayBe<JaminanType>
{

}
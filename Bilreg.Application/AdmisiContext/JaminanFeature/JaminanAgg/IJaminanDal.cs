using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public interface IJaminanDal :
    IInsert<JaminanType>,
    IUpdate<JaminanType>,
    IDelete<IJaminanKey>,
    IGetDataMayBe<JaminanType, IJaminanKey>,
    IListDataMayBe<JaminanType>
{

}
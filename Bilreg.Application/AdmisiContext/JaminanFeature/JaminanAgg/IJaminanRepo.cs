using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;

public interface IJaminanRepo : 
    ILoadEntity<JaminanType, IJaminanKey>,
    IListData<JaminanType>
{
}

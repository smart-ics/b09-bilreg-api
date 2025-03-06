using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public interface IJaminanDal:
    IInsert<JaminanModel>,
    IUpdate<JaminanModel>,
    IDelete<IJaminanKey>,
    IGetData2<JaminanModel, IJaminanKey>,
    IListData2<JaminanModel>
{
    
}
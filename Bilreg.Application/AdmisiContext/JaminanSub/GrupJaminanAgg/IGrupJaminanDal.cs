using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public interface IGrupJaminanDal:
    IInsert<GrupJaminanModel>,
    IUpdate<GrupJaminanModel>,
    IDelete<IGrupJaminanKey>,
    IGetData<GrupJaminanModel, IGrupJaminanKey>,
    IListData<GrupJaminanModel>
{
}
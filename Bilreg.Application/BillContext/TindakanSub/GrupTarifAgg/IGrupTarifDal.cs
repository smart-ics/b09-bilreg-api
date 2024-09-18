using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;

public interface IGrupTarifDal:
    IInsert<GrupTarifModel>,
    IUpdate<GrupTarifModel>,
    IDelete<IGrupTarifKey>,
    IGetData<GrupTarifModel, IGrupTarifKey>,
    IListData<GrupTarifModel>
{
}
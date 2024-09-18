using Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;

public interface IJenisTarifDal :
    IInsert<JenisTarifModel>,
    IUpdate<JenisTarifModel>,
    IDelete<IJenisTarifKey>,
    IGetData<JenisTarifModel,IJenisTarifKey>,
    IListData<JenisTarifModel>
{
}
using Bilreg.Domain.BillContext.TindakanSub.GrupTarifDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifDkAgg;

public interface IGrupTarifDkDal:
    IGetData<GrupTarifDkModel,IGrupTarifDkKey>,
    IListData<GrupTarifDkModel>
{
    
}
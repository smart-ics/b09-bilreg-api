using Bilreg.Domain.AdmPasienContext.PropinsiAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmPasienContext.PropinsiAgg;

public interface IPropinsiDal :
    IInsert<PropinsiModel>,
    IUpdate<PropinsiModel>,
    IDelete<IPropinsiKey>,
    IGetData<PropinsiModel, IPropinsiKey>,
    IListData<PropinsiModel>
{
}
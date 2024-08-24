using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiSub.PropinsiAgg;

public interface IPropinsiDal :
    IInsert<PropinsiModel>,
    IUpdate<PropinsiModel>,
    IDelete<IPropinsiKey>,
    IGetData<PropinsiModel, IPropinsiKey>,
    IListData<PropinsiModel>
{
}
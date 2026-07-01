using Bilreg.Domain.PasienContext.DemografiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public interface IPropinsiDal :
    IInsert<PropinsiType>,
    IUpdate<PropinsiType>,
    IDelete<IPropinsiKey>,
    IGetDataMayBe<PropinsiType, IPropinsiKey>,
    IListDataMayBe<PropinsiType>
{
}
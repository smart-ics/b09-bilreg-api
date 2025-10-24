using Bilreg.Domain.PasienContext.DemografiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public interface INegaraDal :
    IInsert<NegaraType>,
    IUpdate<NegaraType>,
    IGetDataMayBe<NegaraType, INegaraKey>,
    IListDataMayBe<NegaraType>
{
}

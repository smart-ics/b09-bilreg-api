using Bilreg.Domain.PasienContext.DemografiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public interface IKabupatenDal :
    IInsert<KabupatenType>,
    IUpdate<KabupatenType>,
    IDelete<IKabupatenKey>,
    IGetDataMayBe<KabupatenType, IKabupatenKey>,
    IListDataMayBe<KabupatenType, IPropinsiKey>
{
}
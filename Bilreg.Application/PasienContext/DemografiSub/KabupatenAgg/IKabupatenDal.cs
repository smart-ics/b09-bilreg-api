using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiSub.KabupatenAgg;

public interface IKabupatenDal :
    IInsert<KabupatenModel>,
    IUpdate<KabupatenModel>,
    IDelete<IKabupatenKey>,
    IGetData<KabupatenModel, IKabupatenKey>,
    IListData<KabupatenModel, IPropinsiKey>
{
}
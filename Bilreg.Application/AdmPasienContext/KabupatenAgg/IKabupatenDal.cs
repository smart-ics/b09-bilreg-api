using Bilreg.Domain.AdmPasienContext.KabupatenAgg;
using Bilreg.Domain.AdmPasienContext.PropinsiAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmPasienContext.KabupatenAgg;

public interface IKabupatenDal :
    IInsert<KabupatenModel>,
    IUpdate<KabupatenModel>,
    IDelete<IKabupatenKey>,
    IGetData<KabupatenModel, IKabupatenKey>,
    IListData<KabupatenModel, IPropinsiKey>
{
}
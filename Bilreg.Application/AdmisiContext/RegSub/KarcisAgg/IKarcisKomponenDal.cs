using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public interface IKarcisKomponenDal :
    IInsertBulk<KarcisKomponenModel>,
    IDelete<IKarcisKey>,
    IListData<KarcisKomponenModel, IKarcisKey>
{
}
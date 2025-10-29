using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public interface IKarcisKomponenDal :
    IInsertBulk<KarcisKomponenType>,
    IDelete<IKarcisKey>,
    IListData<KarcisKomponenType, IKarcisKey>
{
}
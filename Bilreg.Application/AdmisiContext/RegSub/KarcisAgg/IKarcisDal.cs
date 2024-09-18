using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public interface IKarcisDal :
    IInsert<KarcisModel>,
    IUpdate<KarcisModel>,
    IDelete<IKarcisKey>,
    IGetData<KarcisModel, IKarcisKey>,
    IListData<KarcisModel>
{
}
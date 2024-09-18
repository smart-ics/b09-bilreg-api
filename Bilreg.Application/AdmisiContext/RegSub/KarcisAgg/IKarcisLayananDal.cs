using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public interface IKarcisLayananDal :
    IInsertBulk<KarcisLayananModel>,
    IDelete<IKarcisKey>,
    IListData<KarcisLayananModel, IKarcisKey>
{
}
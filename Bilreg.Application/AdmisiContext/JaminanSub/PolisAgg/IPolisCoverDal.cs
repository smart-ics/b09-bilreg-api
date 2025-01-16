using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;

public interface IPolisCoverDal :
    IInsertBulk<PolisCoverModel>,
    IDelete<IPolisKey>,
    IListData<PolisCoverModel, IPolisKey>
{
}
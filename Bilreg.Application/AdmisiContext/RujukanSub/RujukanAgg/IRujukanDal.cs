using Bilreg.Domain.AdmisiContext.RujukanSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;

public interface IRujukanDal :
    IGetDataMayBe<RujukanType, IRujukanKey>,
    IListDataMayBe<RujukanType>
{
}

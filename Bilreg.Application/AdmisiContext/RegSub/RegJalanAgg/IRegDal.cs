using Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.RegJalanAgg;

public interface IRegDal :
    IInsert<RegModel>,
    IUpdate<RegModel>,
    IDelete<RegModel>,
    IGetData<RegModel, IRegKey>,
    IListData<RegModel, Periode>
{
}


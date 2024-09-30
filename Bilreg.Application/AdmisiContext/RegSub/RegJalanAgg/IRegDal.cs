using Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.RegJalanAgg;

public interface IRegDal :
    IInsert<AbstractRegModel>,
    IUpdate<AbstractRegModel>,
    IDelete<AbstractRegModel>,
    IGetData<AbstractRegModel, IRegKey>,
    IListData<AbstractRegModel, Periode>
{
}


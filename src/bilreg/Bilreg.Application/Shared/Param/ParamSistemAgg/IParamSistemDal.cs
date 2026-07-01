using Bilreg.Domain.Shared.Param;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.Shared.Param.ParamSistemAgg;

public interface IParamSistemDal :
    IInsert<ParamSistemModel>,
    IUpdate<ParamSistemModel>,
    IDelete<IParamSistemKey>,
    IGetData<ParamSistemModel, string>,
    IListData<ParamSistemModel>
{
}
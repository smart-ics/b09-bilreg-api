using Farinv.Domain.Shared.Param;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.ParamContext.ParamSistemAgg;

public interface IParamSistemDal :
    IInsert<ParamSistemModel>,
    IUpdate<ParamSistemModel>,
    IDelete<IParamSistemKey>,
    IGetData<ParamSistemModel, string>,
    IListData<ParamSistemModel>
{
}
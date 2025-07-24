using Bilreg.Domain.ParamContext;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.Helpers;

public interface IParamSistemDal :
    IInsert<ParamSistemModel>,
    IUpdate<ParamSistemModel>,
    IDelete<IParamSistemKey>,
    IGetData<ParamSistemModel, string>,
    IListData<ParamSistemModel>
{
}
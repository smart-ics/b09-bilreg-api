using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.StandardFeature;

public interface IFornasRepo :
    ILoadEntity<FornasType, IFornasKey>,
    IListData<FornasType>
{
}
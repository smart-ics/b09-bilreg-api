using Bilreg.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.StandardFeature;

public interface IFornasRepo :
    ILoadEntity<FornasType, IFornasKey>,
    IListData<FornasType>
{
}
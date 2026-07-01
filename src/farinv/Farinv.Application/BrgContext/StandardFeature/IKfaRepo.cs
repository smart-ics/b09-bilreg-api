using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.StandardFeature;

public interface IKfaRepo :
    ILoadEntity<KfaType, IKfaKey>,
    IListData<KfaType, string>
{
}

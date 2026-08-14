using Bilreg.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.StandardFeature;

public interface IKfaRepo :
    ILoadEntity<KfaType, IKfaKey>,
    IListData<KfaType, string>
{
}

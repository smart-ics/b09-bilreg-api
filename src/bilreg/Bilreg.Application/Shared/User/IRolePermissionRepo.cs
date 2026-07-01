using Bilreg.Domain.Shared.User;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.Shared.User;

public interface IRolePermissionRepo :
    ILoadEntity<RolePermissionModel, IRoleKey>,
    IListData<RolePermissionModel>
{
}

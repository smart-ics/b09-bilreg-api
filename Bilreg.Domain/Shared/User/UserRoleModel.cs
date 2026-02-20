using System.Data;
using System.Security;
using System.Text;

namespace Bilreg.Domain.Shared.User;

public class UserRoleModel : IUserKey
{
    public UserRoleModel(string userId, string userName, string roleId, string roleName)
    {
        UserId = userId;
        UserName = userName;
        RoleId = roleId;
        RoleName = roleName;
    }

    public string UserId { get; private set; }
    public string UserName { get; private set; }

    public string RoleId { get; private set; }
    public string RoleName { get; private set; }


}
using System.Security;

namespace Bilreg.Domain.Shared.User;

public class RoleModel : IRoleKey
{
    #region CREATION
    public RoleModel(string id, string name)
    {
        RoleId = id;
        RoleName = name;
    }
    public static IRoleKey Key(string id) => new RoleModel(id, "-");
    public static RoleModel Default => new RoleModel("-", "-");
    #endregion

    #region PROPERTIES
    public string RoleId { get; private set; }
    public string RoleName { get; private set; }
    #endregion


}


public interface IRoleKey
{ string RoleId { get; } }

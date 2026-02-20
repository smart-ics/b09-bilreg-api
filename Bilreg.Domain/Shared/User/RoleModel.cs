using System.Security;

namespace Bilreg.Domain.Shared.User;

public class RoleModel : IRoleKey
{
    #region CREATION
    public RoleModel(string id, string code, string name, bool isActive)
    {
        RoleId = id;
        RoleCode = code;
        RoleName = name;
        IsActive = isActive;
    }
    public static IRoleKey Key(string id) => new RoleModel(id, "-", "-", true);
    public static RoleModel Default => new RoleModel("-", "-", "-", true);
    #endregion

    #region PROPERTIES
    public string RoleId { get; private set; }
    public string RoleCode { get; private set; }
    public string RoleName { get; private set; }
    public bool IsActive { get; private set; }
    #endregion


    #region BEHAVIOR

    public void Deactivate()
    {
        IsActive = false;
    }
    #endregion
}


public interface IRoleKey
{ string RoleId { get; } }
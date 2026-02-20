namespace Bilreg.Domain.Shared.User;

public class RolePermissionModel : IRoleKey
{
    private readonly List<PermissionModel> _permissions = new();
    #region CREATION
    public RolePermissionModel(string roleId, string roleName, IEnumerable<PermissionModel> permissions)
    {
        RoleId = roleId;
        RoleName = roleName;
        _permissions = permissions?.ToList() ?? [] ;
    }

    public static RolePermissionModel Create(string roleId, string romeName, IEnumerable<PermissionModel> permissions)
    {
        var result = new RolePermissionModel(roleId, romeName, permissions);
        return result;
    }

    public static RolePermissionModel Default => new RolePermissionModel("-", "-", []);

    #endregion


    #region PROPERTIES
    public string RoleId { get; private set; }
    public string RoleName { get; private set; }

    public IEnumerable<PermissionModel> Permissions => _permissions;

    #endregion

    #region BEHAVIOR

    public void AddPermission(PermissionModel permission)
    {
        if (_permissions.Any(p => p.PermissionId == permission.PermissionId))
            return;

        _permissions.Add(new PermissionModel(permission.PermissionId, permission.PermissionCode));
    }

    public void RemovePermission(string permissionId)
    {
        var existing = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existing != null)
            _permissions.Remove(existing);
    }

    public bool HasPermission(string permissionCode)
    {
        return _permissions.Any(p => p.PermissionId == permissionCode);
    }
    #endregion
}

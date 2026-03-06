using System.Data;

namespace Bilreg.Domain.Shared.User;

public class UserModel : IUserKey
{
    #region CREATION
    public UserModel(string userId, string email, string userName, bool isActive, 
        IEnumerable<UserRoleModel> roles)
    {
        UserId = userId;
        Email = email;
        UserName = userName;
        IsActive = isActive;
        _roles = roles.ToList() ?? [];
    }
    public static UserModel Create(string email, string userName, IEnumerable<UserRoleModel> roles)
    {
        var id = Guid.NewGuid();
        var result = new UserModel(id.ToString(), email, userName, true, roles);
        return result;
    }
    public static UserModel Default => new UserModel("-", "-", "-", false, []);
    public static IUserKey Key(string id) => new UserModel("-", id, "-", false, []);

    #endregion

    #region PROPERTIES
    private readonly List<UserRoleModel> _roles;

    

    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string UserName { get; private set; }
    public bool IsActive { get; private set; }

    public IEnumerable<UserRoleModel> ListRole => _roles;

    #endregion

    #region BEHAVIOR

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void AssignRole(RoleModel role)
    {
        if (_roles.Any(r => r.Role.RoleId == role.RoleId))
            return;

        _roles.Add(new UserRoleModel(UserId, Email, UserName, role));
    }

    public void RemoveRole(string roleId)
    {
        var existing = _roles.FirstOrDefault(r => r.Role.RoleId == roleId);
        if (existing != null)
            _roles.Remove(existing);
    }

    public bool HasRole(string roleId)
    {
        return _roles.Any(r => r.Role.RoleId == roleId);
    }
    #endregion
}

public interface IUserKey
{
    string Email { get;  }
}
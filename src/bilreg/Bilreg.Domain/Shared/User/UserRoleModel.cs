namespace Bilreg.Domain.Shared.User;

public class UserRoleModel : IUserKey
{
    public UserRoleModel(string userId, string email, string userName, RoleModel role)
    {
        UserId = userId;
        Email = email;
        UserName = userName;
        Role = role;
    }

    public string UserId { get; private set; }
    public string Email { get; private set; }
    public string UserName { get; private set; }

    public RoleModel Role { get; private set; }
}
//using Bilreg.Domain.Shared.User;
//using System.Security.Cryptography.X509Certificates;

//namespace Bilreg.Application.Shared.Security;

//public class PermissionChecker : IPermissionChecker
//{
//    private readonly IUserRepo _userRepo;
//    private readonly IRoleRepo _roleRepo;
//    public PermissionChecker(IUserRepo userRepo, IRoleRepo roleRepo)
//    {
//        _userRepo = userRepo;
//        _roleRepo = roleRepo;
//    }

//    public async Task<bool> HasPermissionAsync(string userId, string roleId, string permissionCode)
//    {
//        var userKey = UserModel.Key(userId);
//        var user = _userRepo.LoadEntity(userKey).GetValueOrDefault();

//        if (user.UserId == "-" || !user.IsActive)
//            return false;
//        var roleKey = RoleModel.Key(roleId);
//        var role = _roleRepo.LoadEntity(roleKey).GetValueOrDefault();

//        var permissions = role.Permissions.Select(x => x.PermissionId);
            
//        return permissions.Contains(permissionCode);
//    }
//}

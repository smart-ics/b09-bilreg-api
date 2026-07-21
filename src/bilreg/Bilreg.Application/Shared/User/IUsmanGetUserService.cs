using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.Shared.User;

public interface IUsmanGetUserService : INunaService<UsmanGetUserResponse, UsmanGetUserRequest>
{
}

public record UsmanGetUserRequest(string Email, string Pass,string AppId);

public record UsmanGetUserResponse(
    string pegId,
    string userName,
    string UserLogin,
    string email,
    string expiredDate,
    IEnumerable<UsmanGetUserRoleResponse> listRole);

public record UsmanGetUserRoleResponse(string Role);


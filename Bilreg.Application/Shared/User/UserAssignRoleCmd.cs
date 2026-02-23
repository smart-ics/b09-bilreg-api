using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.User;
using MediatR;

namespace Bilreg.Application.Shared.User;

public record UserAssignRoleCmd(string Email, UserRoleReq Role) : IRequest, IUserKey;

public record UserRoleReq(string RoleId, string RoleName);

public class UserAssignRoleHandler : IRequestHandler<UserAssignRoleCmd>
{
    private readonly IUserRepo _userRepo;
    public UserAssignRoleHandler(IUserRepo userRepo)
    {
        _userRepo = userRepo;
    }
    public Task Handle(UserAssignRoleCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Email);
        Guard.Against.Null(request.Role);

        var user = _userRepo.LoadEntity(request).GetValueOrThrow($"User {request.Email} not found");
        if(user.HasRole(request.Role.RoleId))
            return Task.CompletedTask;
        
        var roleNew = new RoleModel(request.Role.RoleId, request.Role.RoleName);
        user.AssignRole(roleNew);

        _userRepo.SaveChanges(user);
        return Task.CompletedTask;
    }
}

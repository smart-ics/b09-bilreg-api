using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.Shared.User;

public record UserGetUserUsmanQuery(string Email, string Pass, string AppId) : 
    IRequest<UserGetUserUsmanResponse>;

public record UserGetUserUsmanResponse(string PegId,
    string UserName,
    string UserLogin,
    string Email,
    string ExpiredDate,
    string TokenAuth,
    IEnumerable<UserGetUserUsmanRoleResp> ListRole);
public record UserGetUserUsmanRoleResp(string Role);

public class UserGetUserUsmanHandler : IRequestHandler<UserGetUserUsmanQuery, UserGetUserUsmanResponse>
{
    private readonly IUsmanGetUserService _usmanGetUserService;
    public UserGetUserUsmanHandler(IUsmanGetUserService usmanGetUserService)
    {
        _usmanGetUserService = usmanGetUserService;
    }

    public Task<UserGetUserUsmanResponse> Handle(UserGetUserUsmanQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Email);
        Guard.Against.NullOrWhiteSpace(request.Pass);
        Guard.Against.NullOrWhiteSpace(request.AppId);

        var payloadReq = new UsmanGetUserRequest(request.Email, request.Pass, request.AppId);
        var user = _usmanGetUserService.Execute(payloadReq);


        var result = new UserGetUserUsmanResponse(
            user.pegId, user.userName, user.UserLogin, 
            user.email, user.expiredDate, "", 
            user.listRole
                .Select(x => new UserGetUserUsmanRoleResp(x.Role))
            );

        return Task.FromResult(result);
    }
}

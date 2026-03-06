using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.User;
using MediatR;

namespace Bilreg.Application.Shared.User;

public record UserCreateCmd(string Email, string UserName) : IRequest<UserCreateResponse>, IUserKey;

public record UserCreateResponse(string UserName, string Email);

public class UserCreateHandler : IRequestHandler<UserCreateCmd, UserCreateResponse>
{
    private readonly IUserRepo _userRepo;

    public UserCreateHandler(IUserRepo userRepo)
    {
        _userRepo = userRepo;
    }

    public Task<UserCreateResponse> Handle(UserCreateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserName, nameof(request.UserName));
        Guard.Against.NullOrWhiteSpace(request.Email, nameof(request.Email));
        var userDb = _userRepo.LoadEntity(request).GetValueOrDefault(UserModel.Default);
        if (userDb.Email != "-")
            throw new KeyNotFoundException($"user dengan email {request.Email} sudah ada");

        var userNew = UserModel.Create(request.Email, request.UserName, []);

        _userRepo.SaveChanges(userNew);

        var result = new UserCreateResponse(userNew.UserName, userNew.Email);
        return Task.FromResult(result);
    }
}

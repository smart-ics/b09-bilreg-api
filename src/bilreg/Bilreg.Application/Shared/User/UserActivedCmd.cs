using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.User;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.Shared.User;

public record UserActivedCmd(string Email) : IRequest, IUserKey;

public class UserActivedHandler : IRequestHandler<UserActivedCmd>
{
    private readonly IUserRepo _userRepo;
    public UserActivedHandler(IUserRepo userRepo)
    {
        _userRepo = userRepo;
    }

    public Task Handle(UserActivedCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Email, nameof(request.Email));
        var user = _userRepo.LoadEntity(request).GetValueOrThrow($"User {request.Email} not found");

        user.Activate();
        _userRepo.SaveChanges(user);

        return Task.CompletedTask;    }
}

using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.User;
using MediatR;

namespace Bilreg.Application.Shared.User;

public record UserDeAvtiveCmd(string Email) : IRequest, IUserKey;

public class UserDeActiveHandler : IRequestHandler<UserDeAvtiveCmd>
{
    private readonly IUserRepo _userRepo;

    public UserDeActiveHandler(IUserRepo userRepo)
    {
        _userRepo = userRepo;
    }

    public Task Handle(UserDeAvtiveCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Email, nameof(request.Email));
        var user = _userRepo.LoadEntity(request).GetValueOrThrow($"User {request.Email} not found");

        user.Deactivate();
        _userRepo.SaveChanges(user);
        
        return Task.CompletedTask;
    }
}

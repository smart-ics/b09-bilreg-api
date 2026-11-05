using Bilreg.Application.Shared;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

internal sealed class FixedCurrentUserContext : ICurrentUserContext
{
    private readonly string _userId;
    private readonly IReadOnlyList<string> _roles;

    public FixedCurrentUserContext(string userId = "TEST-USER", params string[] roles)
    {
        _userId = userId;
        _roles = roles ?? [];
    }

    public string GetActorUserId() => _userId;

    public IReadOnlyList<string> GetRoles() => _roles;

    public string GetClientIpAddress() => "127.0.0.1";
}

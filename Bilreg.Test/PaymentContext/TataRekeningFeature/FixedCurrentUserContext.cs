using Bilreg.Application.Shared;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

internal sealed class FixedCurrentUserContext(string userId = "TEST-USER") : ICurrentUserContext
{
    public string GetActorUserId() => userId;
}

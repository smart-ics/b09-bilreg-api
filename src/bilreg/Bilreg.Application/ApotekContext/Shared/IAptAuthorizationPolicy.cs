namespace Bilreg.Application.ApotekContext.Shared;

public interface IAptAuthorizationPolicy
{
    void AssertCommandAllowed(string commandName, string actorUserId);
}

public class AuthenticatedActorAuthorizationPolicy : IAptAuthorizationPolicy
{
    public void AssertCommandAllowed(string commandName, string actorUserId)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            throw new UnauthorizedAccessException(
                $"Authenticated actor is required for {commandName} (RELEASE-BLOCKED: {ApotekReleaseGates.Bc12CommandRoleMatrix}).");
    }
}

namespace Bilreg.Infrastructure.IgdContext.Integration;

/// <summary>
/// Acquires and caches the SMASS JWT used as the outbound Bearer credential
/// (architecture §9.1, AR-04). Returns <c>null</c> when credentials are not
/// configured; throws when the token endpoint is reachable but unusable.
/// </summary>
public interface ISmassTokenService
{
    Task<string?> GetToken(CancellationToken cancellationToken = default);
}

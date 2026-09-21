using System.Text.Json;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Bilreg.Infrastructure.IgdContext.Integration;

/// <summary>
/// Obtains a JWT from SMASS and caches it in <see cref="IMemoryCache"/> under
/// <see cref="CacheKey"/> with an absolute expiry of <c>exp − 60 s</c>
/// (architecture §9.1, AR-04).
/// </summary>
public class SmassTokenService : ISmassTokenService
{
    internal const string CacheKey = "SmassToken";

    /// <summary>
    /// Safety margin subtracted from the token <c>exp</c> claim (architecture §9.1).
    /// </summary>
    private const int ExpirySafetySeconds = 60;

    /// <summary>Defensive fallback when the token has no parseable <c>exp</c> claim.</summary>
    private static readonly TimeSpan FallbackTtl = TimeSpan.FromMinutes(1);

    private static readonly JsonSerializerOptions JsonOption = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly SmassOptions _opt;
    private readonly IRestClientFactory _restClientFactory;
    private readonly IMemoryCache _cache;

    public SmassTokenService(
        IOptions<SmassOptions> opt,
        IRestClientFactory restClientFactory,
        IMemoryCache cache)
    {
        _opt = opt.Value;
        _restClientFactory = restClientFactory;
        _cache = cache;
    }

    public async Task<string?> GetToken(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
            return cached;

        if (string.IsNullOrWhiteSpace(_opt.BaseApiUrl)
            || string.IsNullOrWhiteSpace(_opt.TokenEmail)
            || string.IsNullOrWhiteSpace(_opt.TokenPass))
            return null;

        var client = _restClientFactory.Create(_opt.BaseApiUrl);

        // Architecture §9.1 body shape: { email, pass } (UsmanLoginCommand).
        // SMASS TokenController is mounted at api/Token (route "api/[controller]").
        var request = new RestRequest(GetTokenRoute, Method.Post)
            .AddStringBody(
                JsonSerializer.Serialize(new { email = _opt.TokenEmail, pass = _opt.TokenPass }, JsonOption),
                DataFormat.Json);

        var response = await client.ExecutePostAsync(request, cancellationToken);
        if (!response.IsSuccessful)
            throw new InvalidOperationException(DescribeFailure(response));

        var token = ParseToken(response.Content);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("SMASS token tidak terbaca.");

        CacheToken(token);
        return token;
    }

    /// <summary>
    /// SMASS mounts its <c>TokenController</c> at <c>api/Token</c>; the plan /
    /// architecture text writes this as <c>{Smass:BaseApiUrl}/Token</c>, which is
    /// inconsistent with the <c>/api</c> prefix used by every other SMASS route and
    /// with the SMASS ecosystem token convention (precedent
    /// <c>UsmanGetTokenService</c> → <c>/api/token</c>). The functional route is used.
    /// </summary>
    private static string GetTokenRoute => "/api/Token";

    private void CacheToken(string token)
    {
        var expiresAt = TryReadExpiry(token) ?? DateTimeOffset.UtcNow.Add(FallbackTtl);
        var absoluteExpiry = expiresAt.AddSeconds(-ExpirySafetySeconds);
        if (absoluteExpiry <= DateTimeOffset.UtcNow)
            return;

        _cache.Set(CacheKey, token, new MemoryCacheEntryOptions { AbsoluteExpiration = absoluteExpiry });
    }

    /// <summary>
    /// Parses the token from the SMASS response. SMASS returns the raw JWT as a JSON
    /// string (<c>Ok(jwt)</c>); the JSend envelope is tolerated per architecture §9.1.
    /// </summary>
    private static string? ParseToken(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var trimmed = content.Trim();

        if (trimmed.StartsWith('{'))
        {
            var envelope = trimmed.DeserializeOrThrow<JSend<string>>(
                $"Parsing failed: {trimmed}", JsonOption);
            return string.IsNullOrWhiteSpace(envelope?.Data) ? null : envelope.Data;
        }

        if (trimmed.StartsWith('"'))
            return JsonSerializer.Deserialize<string>(trimmed, JsonOption);

        return trimmed;
    }

    /// <summary>Reads the JWT <c>exp</c> claim (Unix seconds) without external packages.</summary>
    private static DateTimeOffset? TryReadExpiry(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            var remainder = payload.Length % 4;
            if (remainder == 1)
                return null;
            if (remainder == 2)
                payload += "==";
            else if (remainder == 3)
                payload += "=";

            var json = Convert.FromBase64String(payload);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("exp", out var exp)
                && exp.TryGetInt64(out var unixSeconds))
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string DescribeFailure(RestResponse response)
    {
        var detail = string.IsNullOrWhiteSpace(response.ErrorMessage)
            ? response.StatusDescription
            : response.ErrorMessage;
        return $"SMASS token gagal: HTTP {(int)response.StatusCode} {detail}";
    }
}

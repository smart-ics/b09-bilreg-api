using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.Shared.User;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Test.PaymentContext.TataRekeningFeature.Api;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Bilreg.Test.Authentication;

/// <summary>
/// Regression guard for the JWT authentication fix.
///
/// Root cause (fixed): UserController.GetToken wrote the <c>iat</c> claim as a
/// human-readable date string instead of an RFC 7519 NumericDate, which made every
/// token unreadable by the JWT Bearer middleware (401 invalid_token).
///
/// These tests fail on the pre-fix code (login token is rejected -> 401) and pass afterward.
/// </summary>
public class JwtAuthenticationRegressionTest : IClassFixture<JwtAuthWebApplicationFactory>
{
    private const string ExpectedIssuer = "BilregApiServer";
    private const string ExpectedAudience = "BilregApiClient";
    private const string ExpectedSubject = "BilregApiAccessToken";
    private const string SigningKey = "ErF7Zq0praAgc4pV7ajVG4h2rAmP99bvgLMyCVWGkq0=";

    private readonly JwtAuthWebApplicationFactory _factory;

    public JwtAuthenticationRegressionTest(JwtAuthWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.Harness.Reset();
        _factory.UsmanGetUserService.Reset();
    }

    [Fact]
    public async Task Login_Then_CallProtectedEndpoint_Returns200()
    {
        SetupUsmanUser();
        _factory.Harness.SetupVerifyScenario();

        TataRekeningModel? savedModel = null;
        _factory.Harness.TataRekeningRepo
            .Setup(r => r.SaveChanges(It.IsAny<TataRekeningModel>()))
            .Callback<TataRekeningModel>(m => savedModel = m);

        var client = _factory.CreateClient();

        // 1. Login -> JWT is generated locally by Bilreg.Api after USMAN validates credentials.
        var token = await LoginAndGetTokenAsync(client);

        // 2. Verify JWT payload shape (alg / iss / aud / sub / iat / exp).
        AssertTokenPayloadIsValid(token);

        // 3. Call a protected endpoint using the freshly issued token.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/verify",
            new { action = FinancialVerificationAction.Verify });

        // 4. Protected endpoint returns 200 OK instead of 401 Unauthorized.
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. ICurrentUserContext resolved the authenticated principal from the JWT and the
        //    actor id flowed into the domain (proves HttpContext.User was authenticated).
        savedModel.Should().NotBeNull();
        savedModel!.FinancialVerificationInfo.Should().NotBeNull();
        savedModel.FinancialVerificationInfo!.PetugasVerif.Should().Be(ExpectedSubject);
    }

    [Fact]
    public async Task Login_IssuedToken_ValidatesAgainstProductionParameters()
    {
        SetupUsmanUser();
        var client = _factory.CreateClient();

        var token = await LoginAndGetTokenAsync(client);

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = ExpectedIssuer,
            ValidAudience = ExpectedAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey))
        };

        var act = () => handler.ValidateToken(token, validationParameters, out _);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithLegacyStringIatToken_Returns401()
    {
        // Reproduces the original defect: a token whose iat is a date string is rejected.
        var badToken = CreateTokenWithStringIat();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", badToken);

        var response = await client.PostAsJsonAsync(
            $"/api/tatarekening/{TataRekeningApiTestHarness.RegId}/verify",
            new { action = FinancialVerificationAction.Verify });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString()
            .Should().Contain("invalid_token", "the malformed iat claim must be rejected as an invalid token");
    }

    private void SetupUsmanUser() =>
        _factory.UsmanGetUserService
            .Setup(s => s.Execute(It.IsAny<UsmanGetUserRequest>()))
            .Returns(new UsmanGetUserResponse(
                pegId: "PEG-001",
                userName: "Dr. Test Verifikator",
                UserLogin: "drtest",
                email: "drtest@example.com",
                expiredDate: "2027-01-01",
                listRole: new[] { new UsmanGetUserRoleResponse("VERIF-SPV") }));

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "/login",
            new { email = "drtest@example.com", pass = "secret", appId = "BILREG" });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "login should succeed");

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("data").GetProperty("tokenAuth").GetString();
        token.Should().NotBeNullOrWhiteSpace("login must return a JWT");
        return token!;
    }

    private static void AssertTokenPayloadIsValid(string token)
    {
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "a signed JWT has header.payload.signature");

        using var header = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[0])));
        header.RootElement.GetProperty("alg").GetString().Should().Be("HS256");

        using var payload = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));
        var root = payload.RootElement;

        root.GetProperty("iss").GetString().Should().Be(ExpectedIssuer);
        root.GetProperty("aud").GetString().Should().Be(ExpectedAudience);
        root.GetProperty("sub").GetString().Should().Be(ExpectedSubject);

        // The core fix: iat must be an integer NumericDate, not a string date.
        var iat = root.GetProperty("iat");
        iat.ValueKind.Should().Be(JsonValueKind.Number, "iat must be an RFC 7519 NumericDate (Unix seconds)");
        iat.GetInt64().Should().BeGreaterThan(0);

        var exp = root.GetProperty("exp");
        exp.ValueKind.Should().Be(JsonValueKind.Number);
        exp.GetInt64().Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private static string CreateTokenWithStringIat()
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, ExpectedSubject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, "Dr. Test Verifikator")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            ExpectedIssuer,
            ExpectedAudience,
            claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}

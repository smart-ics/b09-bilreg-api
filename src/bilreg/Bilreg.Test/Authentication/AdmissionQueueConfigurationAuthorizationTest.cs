using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Bilreg.Application.Shared.User;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Bilreg.Test.Authentication;

public class AdmissionQueueConfigurationAuthorizationTest : IClassFixture<JwtAuthWebApplicationFactory>
{
    private const string SigningKey = "ErF7Zq0praAgc4pV7ajVG4h2rAmP99bvgLMyCVWGkq0=";
    private const string ExpectedIssuer = "BilregApiServer";
    private const string ExpectedAudience = "BilregApiClient";
    private const string ExpectedSubject = "BilregApiAccessToken";

    private readonly JwtAuthWebApplicationFactory _factory;

    public AdmissionQueueConfigurationAuthorizationTest(JwtAuthWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.UsmanGetUserService.Reset();
    }

    [Fact]
    public async Task ConfigurationWhoAmI_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admission-queue/configuration/whoami");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfigurationKiosks_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/admission-queue/configuration/kiosks");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfigurationWhoAmI_WithUnauthorizedRole_Returns403()
    {
        SetupUsmanUser("VERIF-USR");
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admission-queue/configuration/whoami");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        // Nuna JSend stores the AQ identifier in `status`; numeric HTTP status is in `code`.
        document.RootElement.GetProperty("status").GetString().Should().Be("AQ_CONFIG_FORBIDDEN");
    }

    [Fact]
    public async Task ConfigurationWhoAmI_WithAllowedRole_Returns200()
    {
        SetupUsmanUser("ADM-SPV");
        var client = _factory.CreateClient();
        var token = await LoginAndGetTokenAsync(client);
        AssertTokenContainsRole(token, "ADM-SPV");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/v1/admission-queue/configuration/whoami");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfigurationWhoAmI_WithPermissionClaim_Returns200()
    {
        var token = CreateTokenWithPermissionClaim("AdmissionQueueConfiguration");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admission-queue/configuration/whoami");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void SetupUsmanUser(string role) =>
        _factory.UsmanGetUserService
            .Setup(s => s.Execute(It.IsAny<UsmanGetUserRequest>()))
            .Returns(new UsmanGetUserResponse(
                pegId: "PEG-CFG-001",
                userName: "Config Admin",
                UserLogin: "cfgadmin",
                email: "cfgadmin@example.com",
                expiredDate: "2027-01-01",
                listRole: new[] { new UsmanGetUserRoleResponse(role) }));

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync(
            "/login",
            new { email = "cfgadmin@example.com", pass = "secret", appId = "BILREG" });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("data").GetProperty("tokenAuth").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        return token!;
    }

    private static void AssertTokenContainsRole(string token, string role)
    {
        var parts = token.Split('.');
        using var payload = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));
        var root = payload.RootElement;
        var roleClaimType = ClaimTypes.Role;
        var found = false;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name == "role" || property.Name == roleClaimType ||
                property.Name.EndsWith("/role", StringComparison.OrdinalIgnoreCase))
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    found = property.Value.EnumerateArray().Any(x => x.GetString() == role);
                }
                else
                {
                    found = property.Value.GetString() == role;
                }
            }
        }
        found.Should().BeTrue($"JWT should embed role claim {role}");
    }

    private static string CreateTokenWithPermissionClaim(string permission)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, ExpectedSubject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(ClaimTypes.Email, "perm@example.com"),
            new(ClaimTypes.Name, "Permission User"),
            new("permission", permission)
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

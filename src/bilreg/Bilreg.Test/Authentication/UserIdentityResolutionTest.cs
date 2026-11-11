using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bilreg.Api.Authorization;
using Bilreg.Api.Controllers;
using Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature;
using Bilreg.Application.Shared.User;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Bilreg.Test.Authentication;

public class UserIdentityResolutionTest
{
    private readonly IConfiguration _configuration;

    public UserIdentityResolutionTest()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Subject", "BilregApiAccessToken" },
            { "Jwt:Issuer", "BilregApiServer" },
            { "Jwt:Audience", "BilregApiClient" },
            { "Jwt:Key", "ErF7Zq0praAgc4pV7ajVG4h2rAmP99bvgLMyCVWGkq0=" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void UserController_GetToken_PopulatesClaimsFromPegId()
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new UserController(mediatorMock.Object, _configuration);
        var response = new UserGetUserUsmanResponse(
            PegId: "PEG-001",
            UserName: "Dr. Strange",
            UserLogin: "strange_usr",
            Email: "strange@example.com",
            ExpiredDate: "2027-01-01",
            TokenAuth: string.Empty,
            ListRole: new[] { new UserGetUserUsmanRoleResp("VERIF-SPV") });

        var tokenString = controller.GetToken("strange@example.com", response);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be("PEG-001");
        token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be("PEG-001");
        token.Claims.First(c => c.Type == "pegId").Value.Should().Be("PEG-001");
        token.Claims.First(c => c.Type == ClaimTypes.Name).Value.Should().Be("Dr. Strange");
        token.Claims.First(c => c.Type == ClaimTypes.Email).Value.Should().Be("strange@example.com");

        token.Claims.Should().NotContain(c => c.Value == "BilregApiAccessToken");
    }

    [Fact]
    public void UserController_GetToken_FallsBackToUserLogin_WhenPegIdMissing()
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new UserController(mediatorMock.Object, _configuration);
        var response = new UserGetUserUsmanResponse(
            PegId: "   ",
            UserName: "Dr. Strange",
            UserLogin: "strange_usr",
            Email: "strange@example.com",
            ExpiredDate: "2027-01-01",
            TokenAuth: string.Empty,
            ListRole: Array.Empty<UserGetUserUsmanRoleResp>());

        var tokenString = controller.GetToken("strange@example.com", response);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be("strange_usr");
        token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be("strange_usr");
        token.Claims.First(c => c.Type == "pegId").Value.Should().Be("strange_usr");
    }

    [Fact]
    public void UserController_GetToken_FallsBackToUserName_WhenPegIdAndUserLoginMissing()
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new UserController(mediatorMock.Object, _configuration);
        var response = new UserGetUserUsmanResponse(
            PegId: string.Empty,
            UserName: "Dr Strange",
            UserLogin: string.Empty,
            Email: "strange@example.com",
            ExpiredDate: "2027-01-01",
            TokenAuth: string.Empty,
            ListRole: Array.Empty<UserGetUserUsmanRoleResp>());

        var tokenString = controller.GetToken("strange@example.com", response);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

        token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be("Dr Strange");
        token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be("Dr Strange");
        token.Claims.First(c => c.Type == "pegId").Value.Should().Be("Dr Strange");
    }

    [Fact]
    public void HttpCurrentUserContext_GetActorUserId_PrioritizesPegId()
    {
        var claims = new[]
        {
            new Claim("pegId", "PEG-007"),
            new Claim(ClaimTypes.NameIdentifier, "agent007"),
            new Claim(ClaimTypes.Name, "James Bond")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new HttpCurrentUserContext(accessorMock.Object);

        context.GetActorUserId().Should().Be("PEG-007");
    }

    [Fact]
    public void HttpCurrentUserContext_GetActorUserId_IgnoresBilregApiAccessTokenSubject()
    {
        var claims = new[]
        {
            new Claim("sub", "BilregApiAccessToken"),
            new Claim(ClaimTypes.NameIdentifier, "BilregApiAccessToken"),
            new Claim(ClaimTypes.Name, "RealPetugas")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new HttpCurrentUserContext(accessorMock.Object);

        context.GetActorUserId().Should().Be("RealPetugas");
    }

    [Fact]
    public void HttpCurrentUserContext_GetActorUserId_ThrowsWhenAllClaimsAreSystemDefault()
    {
        var claims = new[]
        {
            new Claim("sub", "BilregApiAccessToken"),
            new Claim(ClaimTypes.NameIdentifier, "BilregApiAccessToken")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var context = new HttpCurrentUserContext(accessorMock.Object);

        var act = () => context.GetActorUserId();
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void TataRekeningController_ResolveUserId_UsesExplicitUserId_WhenProvided()
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new TataRekeningController(mediatorMock.Object);

        controller.ResolveUserId("PEG-EXPLICIT").Should().Be("PEG-EXPLICIT");
    }

    [Fact]
    public void TataRekeningController_ResolveUserId_IgnoresBilregApiAccessTokenInPayload()
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new TataRekeningController(mediatorMock.Object);

        var claims = new[]
        {
            new Claim("pegId", "PEG-FROM-CLAIM")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        controller.ResolveUserId("BilregApiAccessToken").Should().Be("PEG-FROM-CLAIM");
    }

    [Fact]
    public void TataRekeningController_ResolveUserId_PrioritizesPegIdClaim_OverOtherClaims()
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new TataRekeningController(mediatorMock.Object);

        var claims = new[]
        {
            new Claim("pegId", "PEG-SUPER"),
            new Claim(ClaimTypes.NameIdentifier, "other_usr"),
            new Claim(ClaimTypes.Name, "Other Name")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        controller.ResolveUserId(null).Should().Be("PEG-SUPER");
    }

    [Fact]
    public void TataRekeningController_ResolveUserId_IgnoresBilregApiAccessToken_AndFallsBackToAdmin()
    {
        var mediatorMock = new Mock<IMediator>();
        var controller = new TataRekeningController(mediatorMock.Object);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "BilregApiAccessToken"),
            new Claim(ClaimTypes.Name, "BilregApiAccessToken")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        controller.ResolveUserId(null).Should().Be("admin");
    }
}

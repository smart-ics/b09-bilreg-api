using System.Security.Claims;
using Bilreg.Api.Controllers.PaymentContext.DepositSub;
using Bilreg.Application.PaymentContext.DepositFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DepositController _sut;

    public DepositControllerTest()
    {
        _sut = new DepositController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(DepositController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(DepositController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(DepositController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/[controller]");
    }

    [Fact]
    public async Task Save_GivenCommand_WhenExecuted_ThenSendsCommandToMediatorAndReturnsOk()
    {
        // ARRANGE
        var cmd = new DepositCreateCmd("REG01", "LAY01", "Titipan Uang Muka", 500000m, "USR01");
        var expectedResponse = new DepositCreateResponse("DEP01");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.Save(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_GivenId_WhenExecuted_ThenSendsQueryToMediatorAndReturnsOk()
    {
        // ARRANGE
        const string depositId = "DEP01";
        var expectedResponse = new DepositGetResponse(
            depositId,
            "2026-10-22 10:00:00",
            "USR01",
            new RegReff("REG01", "PAS01", "Pasien A"),
            new LayananReff("LAY01", "Layanan A"),
            "Titipan Uang Muka",
            500000m);
        _mediatorMock
            .Setup(x => x.Send(It.Is<DepositGetQry>(q => q.DepositId == depositId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.Get(depositId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<DepositGetQry>(q => q.DepositId == depositId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListByReg_GivenRegId_WhenExecuted_ThenSendsQueryToMediatorAndReturnsOk()
    {
        // ARRANGE
        const string regId = "REG01";
        var expectedResponse = new List<DepositListResponse>
        {
            new(
                "DEP01",
                "2026-10-22 10:00:00",
                "USR01",
                new RegReff("REG01", "PAS01", "Pasien A"),
                new LayananReff("LAY01", "Layanan A"),
                "Titipan Uang Muka",
                500000m)
        };
        _mediatorMock
            .Setup(x => x.Send(It.Is<DepositListQry>(q => q.RegId == regId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.ListByReg(regId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<DepositListQry>(q => q.RegId == regId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSaldo_GivenRegId_WhenExecuted_ThenSendsDepositSaldoGetQryAndReturnsOk()
    {
        // ARRANGE
        const string regId = "REG01";
        var expectedResponse = new DepositSaldoResponse(
            RegId: regId,
            TotalSetoran: 1000000m,
            TotalTerpakai: 250000m,
            SaldoAktif: 750000m,
            Status: "Aktif");
        _mediatorMock
            .Setup(x => x.Send(It.Is<DepositSaldoGetQry>(q => q.RegId == regId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.GetSaldo(regId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<DepositSaldoGetQry>(q => q.RegId == regId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetSaldo_HasExpectedRouteAndProducesResponseTypeAttributes()
    {
        var method = typeof(DepositController).GetMethod(nameof(DepositController.GetSaldo));
        method.Should().NotBeNull();
        method!.Should().BeDecoratedWith<HttpGetAttribute>();
        method.Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "saldo/{regId}");
        method.Should().BeDecoratedWith<ProducesResponseTypeAttribute>(a => a.StatusCode == StatusCodes.Status200OK);
        method.Should().BeDecoratedWith<ProducesResponseTypeAttribute>(a => a.StatusCode == StatusCodes.Status400BadRequest);
        method.Should().BeDecoratedWith<ProducesResponseTypeAttribute>(a => a.StatusCode == StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Void_GivenIdAndUserIdentityName_WhenExecuted_ThenSendsDepositVoidCmdWithUserIdAndReturnsOk()
    {
        // ARRANGE
        const string depositId = "DEP01";
        const string userId = "PETUGAS01";

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, userId)
                }, "TestAuth"))
            }
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<DepositVoidCmd>(c => c.DepositId == depositId && c.UserId == userId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.Void(depositId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Deposit berhasil dibatalkan.");
        _mediatorMock.Verify(x => x.Send(It.Is<DepositVoidCmd>(c => c.DepositId == depositId && c.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Void_GivenIdAndClaimNameIdentifier_WhenExecuted_ThenSendsDepositVoidCmdWithNameIdentifierAndReturnsOk()
    {
        // ARRANGE
        const string depositId = "DEP02";
        const string userId = "USER_ID_CLAIM";

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "TestAuth"))
            }
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<DepositVoidCmd>(c => c.DepositId == depositId && c.UserId == userId), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.Void(depositId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Deposit berhasil dibatalkan.");
        _mediatorMock.Verify(x => x.Send(It.Is<DepositVoidCmd>(c => c.DepositId == depositId && c.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Void_GivenIdAndNoUserIdentity_WhenExecuted_ThenSendsDepositVoidCmdWithEmptyUserId()
    {
        // ARRANGE
        const string depositId = "DEP03";

        _mediatorMock
            .Setup(x => x.Send(It.Is<DepositVoidCmd>(c => c.DepositId == depositId && c.UserId == string.Empty), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.Void(depositId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Deposit berhasil dibatalkan.");
        _mediatorMock.Verify(x => x.Send(It.Is<DepositVoidCmd>(c => c.DepositId == depositId && c.UserId == string.Empty), It.IsAny<CancellationToken>()), Times.Once);
    }
}

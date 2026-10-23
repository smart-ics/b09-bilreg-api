using System.Reflection;
using System.Security.Claims;
using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OrderOpControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly OrderOpController _sut;

    public OrderOpControllerTest()
    {
        _sut = new OrderOpController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAttribute()
    {
        // ASSERT
        var authorizeAttr = typeof(OrderOpController).GetCustomAttribute<AuthorizeAttribute>(true);
        authorizeAttr.Should().NotBeNull("OrderOpController must be decorated with [Authorize]");
    }

    [Fact]
    public void Controller_ShouldNotHaveCreateByPasienMethod()
    {
        // ASSERT
        var methods = typeof(OrderOpController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        methods.Should().NotContain(m => m.Name.Contains("Pasien", StringComparison.OrdinalIgnoreCase),
            "CreateByPasien endpoint must be completely removed");
    }

    [Fact]
    public async Task CreateOrderByReg_GivenValidCommand_WhenNoUserClaim_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new OkCreateOrderOpByRegCmd(
            RegId: "REG-001",
            DokterPengorderId: "DOC-01",
            LayananPengorderId: "LYN-01",
            DokterOperatorId: "DOC-02",
            DiagCode: "K35.8",
            JenisOperasiId: "JNS-01",
            NamaOperasi: "Apendektomi",
            ProsedurTambahan: "Drainase",
            KlasifikasiOperasi: KlasifikasiOperasiEnum.Sedang,
            AnestesiPlan: AnestesiPlanEnum.General,
            PostOpCareUnit: PostOpCareUnitEnum.PACU,
            TarifId: null,
            EstimasiDurasiInMinutes: 60,
            PreferedDate: new DateTime(2026, 10, 20),
            SpecialEquipment: "-",
            UserId: "nurse-user",
            IsForceCreate: false,
            ListResource: null);

        var expectedResponse = new OkCreateOrderOpByRegResponse(
            "ORD-001",
            false,
            false,
            new OkCreateOrderOpByRegResponseContent("", "", ""),
            new OkCreateOrderOpByRegResponseContent("REG-001", "Apendektomi", "2026-10-20"));

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.CreateOrderByReg(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderByReg_WhenUserClaimExists_ThenOverridesUserIdWithClaimAndReturnsOk()
    {
        // ARRANGE
        const string claimUserId = "claim-user-99";
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("UserId", claimUserId)
        ], "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var cmd = new OkCreateOrderOpByRegCmd(
            RegId: "REG-001",
            DokterPengorderId: "DOC-01",
            LayananPengorderId: "LYN-01",
            DokterOperatorId: "DOC-02",
            DiagCode: "K35.8",
            JenisOperasiId: "JNS-01",
            NamaOperasi: "Apendektomi",
            ProsedurTambahan: "",
            KlasifikasiOperasi: KlasifikasiOperasiEnum.Kecil,
            AnestesiPlan: AnestesiPlanEnum.Lokal,
            PostOpCareUnit: PostOpCareUnitEnum.Ward,
            TarifId: null,
            EstimasiDurasiInMinutes: 30,
            PreferedDate: new DateTime(2026, 10, 20),
            SpecialEquipment: "-",
            UserId: "original-user",
            IsForceCreate: false);

        OkCreateOrderOpByRegCmd? capturedCmd = null;
        var expectedResponse = new OkCreateOrderOpByRegResponse(
            "ORD-002",
            false,
            false,
            new OkCreateOrderOpByRegResponseContent("", "", ""),
            new OkCreateOrderOpByRegResponseContent("REG-001", "Apendektomi", "2026-10-20"));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkCreateOrderOpByRegCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<OkCreateOrderOpByRegResponse>, CancellationToken>((req, _) => capturedCmd = req as OkCreateOrderOpByRegCmd)
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.CreateOrderByReg(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.UserId.Should().Be(claimUserId, "UserId must be forwarded from ClaimsPrincipal");
    }

    [Fact]
    public async Task Cancel_GivenValidCommand_WhenNoUserClaim_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new OkCancelOrderOpCommand("ORD-001", "Kondisi pasien tidak stabil", "doc-01");
        var expectedResponse = new OkCancelOrderOpResponse("ORD-001", "Cancelled", "Kondisi pasien tidak stabil");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Cancel(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenUserClaimExists_ThenOverridesUserIdWithClaimAndReturnsOk()
    {
        // ARRANGE
        const string claimUserId = "auth-cancel-user";
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, claimUserId)
        ], "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var cmd = new OkCancelOrderOpCommand("ORD-001", "Alasan pembatalan klinis", "");
        OkCancelOrderOpCommand? capturedCmd = null;
        var expectedResponse = new OkCancelOrderOpResponse("ORD-001", "Cancelled", "Alasan pembatalan klinis");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkCancelOrderOpCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<OkCancelOrderOpResponse>, CancellationToken>((req, _) => capturedCmd = req as OkCancelOrderOpCommand)
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Cancel(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.UserId.Should().Be(claimUserId, "UserId must be forwarded from ClaimsPrincipal");
    }

    [Fact]
    public async Task Cancel_GivenOrderOpCancelRequest_WhenInvoked_ThenConvertsAndReturnsOk()
    {
        // ARRANGE
        var req = new OrderOpCancelRequest("ORD-001", "Alasan pembatalan via request DTO");
        var expectedResponse = new OkCancelOrderOpResponse("ORD-001", "Cancelled", "Alasan pembatalan via request DTO");

        _mediatorMock
            .Setup(x => x.Send(It.Is<OkCancelOrderOpCommand>(c => c.OrderOpId == req.OrderOpId && c.AlasanBatal == req.AlasanBatal), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.Cancel(req);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<OkCancelOrderOpCommand>(c => c.OrderOpId == req.OrderOpId && c.AlasanBatal == req.AlasanBatal),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListOrder_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        var expectedResponse = new List<OkListOperasiAktifResponse>
        {
            new("ORD-001", "PAS-001", "PASIEN A", "DOC-01", "dr. Pengorder", "DOC-02", "dr. Operator", "Apendektomi", "Drainase", "Sedang", "2026-10-20", 60, "Elective", "Requested")
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkListOperasiAktifQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.ListOrder();

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(It.IsAny<OkListOperasiAktifQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DischargeOp_WhenExecuted_ThenSendsCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new OkDischargeOpCommand("ORD-001", "2026-10-20", "15:00", 1, "KMR-01", "Catatan post-op", "user-01");
        var expectedResponse = new OkDischargeOpResponse("ORD-001", "Discharged", "2026-10-20 15:00", new KamarReff("KMR-01", "Mawar 01"));

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.DischargeOp(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DependencyInjection_WhenAddInfrastructureCalled_ThenRegistersOrderOpResourceDal()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // ACT
        services.AddInfrastructure(configuration);

        // ASSERT
        services.Should().Contain(d =>
            d.ServiceType == typeof(IOrderOpResourceDal) &&
            d.ImplementationType == typeof(OrderOpResourceDal) &&
            d.Lifetime == ServiceLifetime.Scoped,
            "IOrderOpResourceDal must be registered as Scoped OrderOpResourceDal in InfrastructureService");
    }
}

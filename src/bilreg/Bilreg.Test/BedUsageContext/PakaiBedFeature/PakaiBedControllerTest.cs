using System.Net;
using System.Reflection;
using System.Text.Json;
using Bilreg.Api.Controllers.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly PakaiBedController _sut;

    public PakaiBedControllerTest()
    {
        _sut = new PakaiBedController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_HasExpectedAttributes()
    {
        typeof(PakaiBedController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(PakaiBedController).Should().BeDecoratedWith<AuthorizeAttribute>();
        typeof(PakaiBedController).Should().BeDecoratedWith<RouteAttribute>(a => a.Template == "api/[controller]");
    }

    [Fact]
    public void ActionRoutes_HaveExpectedRouteConfigurations()
    {
        var type = typeof(PakaiBedController);

        var createMethod = type.GetMethod(nameof(PakaiBedController.Create));
        createMethod.Should().NotBeNull();
        createMethod!.GetCustomAttributes<HttpPostAttribute>().Should().NotBeEmpty();
        var createRoutes = createMethod.GetCustomAttributes<RouteAttribute>().Select(r => r.Template).ToList();
        createRoutes.Should().Contain("");
        createRoutes.Should().Contain("Create");

        var releaseMethod = type.GetMethod(nameof(PakaiBedController.Release));
        releaseMethod.Should().NotBeNull();
        releaseMethod!.GetCustomAttributes<HttpPostAttribute>().Should().NotBeEmpty();
        releaseMethod!.GetCustomAttributes<RouteAttribute>().Should().Contain(r => r.Template == "release");

        var batalMethod = type.GetMethod(nameof(PakaiBedController.Batal));
        batalMethod.Should().NotBeNull();
        batalMethod!.GetCustomAttributes<HttpPatchAttribute>().Should().NotBeEmpty();
        batalMethod!.GetCustomAttributes<RouteAttribute>().Should().Contain(r => r.Template == "batal");

        var activeBedMethod = type.GetMethod(nameof(PakaiBedController.GetActiveByBed));
        activeBedMethod.Should().NotBeNull();
        activeBedMethod!.GetCustomAttributes<HttpGetAttribute>().Should().NotBeEmpty();
        activeBedMethod!.GetCustomAttributes<RouteAttribute>().Should().Contain(r => r.Template == "bed/{bedId}/active");

        var activeWardMethod = type.GetMethod(nameof(PakaiBedController.ListActiveByWard));
        activeWardMethod.Should().NotBeNull();
        activeWardMethod!.GetCustomAttributes<HttpGetAttribute>().Should().NotBeEmpty();
        activeWardMethod!.GetCustomAttributes<RouteAttribute>().Should().Contain(r => r.Template == "bangsal/{bangsalId}/active");

        var getDataMethod = type.GetMethod(nameof(PakaiBedController.GetData));
        getDataMethod.Should().NotBeNull();
        getDataMethod!.GetCustomAttributes<HttpGetAttribute>().Should().NotBeEmpty();
        getDataMethod!.GetCustomAttributes<RouteAttribute>().Should().Contain(r => r.Template == "{pakaiBedId}");

        var listByRegMethod = type.GetMethod(nameof(PakaiBedController.ListDataByReg));
        listByRegMethod.Should().NotBeNull();
        listByRegMethod!.GetCustomAttributes<HttpGetAttribute>().Should().NotBeEmpty();
        listByRegMethod!.GetCustomAttributes<RouteAttribute>().Should().Contain(r => r.Template == "{regId}/list");
    }

    [Fact]
    public async Task Create_GivenValidCommand_WhenExecuted_ThenSendsCommandWithAllParametersAndReturnsOk()
    {
        // ARRANGE
        var cmd = new PakaiBedCreateCommand(
            RegId: "RG001",
            LayananId: "LY001",
            TipeKamarId: "TK01",
            BedId: "BD001",
            UserId: "USR001",
            TglJamIn: new DateTime(2026, 10, 16, 8, 30, 0, DateTimeKind.Utc),
            BillingKelasId: "KL02",
            WaitingListId: "WL001",
            MotherPakaiBedId: "BE000",
            IsRoomingIn: true,
            IsTitipKelas: true,
            IsNaikKelas: false);

        var expectedResult = new PakaiBedCreateResult("BE001");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.Create(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedCreateCommand>(c =>
                c.RegId == "RG001" &&
                c.BedId == "BD001" &&
                c.BillingKelasId == "KL02" &&
                c.WaitingListId == "WL001" &&
                c.MotherPakaiBedId == "BE000" &&
                c.IsRoomingIn &&
                c.IsTitipKelas &&
                !c.IsNaikKelas),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Release_GivenValidRequest_WhenExecuted_ThenDelegatesToPakaiBedReleaseCommandAndReturnsOk()
    {
        // ARRANGE
        var releaseTime = new DateTime(2026, 10, 16, 14, 0, 0, DateTimeKind.Utc);
        var req = new PakaiBedController.PakaiBedReleaseRequest(
            PakaiBedId: "BE001",
            TglJamOut: releaseTime,
            Reason: "Pasien Pulang Sembuh",
            PetugasOut: "Ns. Ratna");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBedReleaseCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var actionResult = await _sut.Release(req);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedReleaseCommand>(c =>
                c.PakaiBedId == "BE001" &&
                c.TglJamOut == releaseTime &&
                c.Reason == "Pasien Pulang Sembuh" &&
                c.PetugasOut == "Ns. Ratna"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Release_GivenNullRequest_WhenExecuted_ThenReturnsBadRequest()
    {
        // ACT
        var actionResult = await _sut.Release(null!);

        // ASSERT
        var badResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var jsend = badResult.Value.Should().BeAssignableTo<JSendModel>().Subject;
        jsend.status.Should().Be("failed");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PakaiBedReleaseCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Batal_GivenValidRequestWithHttpContext_WhenExecuted_ThenSendsVoidCommandAndReturnsOk()
    {
        // ARRANGE
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "Mozilla/5.0 TestBrowser";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.50");
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var req = new PakaiBedController.PakaiBedVoidRequest(
            PakaiBedId: "BE001",
            UserId: "USR002",
            VoidReason: "Salah input data admisi");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBedVoidCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var actionResult = await _sut.Batal(req);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedVoidCommand>(c =>
                c.PakaiBedId == "BE001" &&
                c.UserId == "USR002" &&
                c.VoidReason == "Salah input data admisi" &&
                c.UserAgent == "Mozilla/5.0 TestBrowser" &&
                c.RemoteIpAddress == "192.168.1.50"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Batal_GivenNullRequest_WhenExecuted_ThenReturnsBadRequest()
    {
        // ACT
        var actionResult = await _sut.Batal(null!);

        // ASSERT
        var badResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var jsend = badResult.Value.Should().BeAssignableTo<JSendModel>().Subject;
        jsend.status.Should().Be("failed");

        _mediatorMock.Verify(x => x.Send(It.IsAny<PakaiBedVoidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Batal_GivenBillingLocked_WhenMediatorThrowsInvalidOperationException_ThenReturnsBadRequestWithClearMessage()
    {
        // ARRANGE
        var req = new PakaiBedController.PakaiBedVoidRequest(
            PakaiBedId: "BE001",
            UserId: "USR002",
            VoidReason: "Batal rawat");

        const string lockErrorMessage = "Pembatalan penempatan tempat tidur BE001 ditolak karena tagihan terkunci: Transaksi roomcharge sudah ada.";
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBedVoidCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(lockErrorMessage));

        // ACT
        var actionResult = await _sut.Batal(req);

        // ASSERT
        var badResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var jsend = badResult.Value.Should().BeAssignableTo<JSendModel>().Subject;
        jsend.status.Should().Be("failed");

        var json = JsonSerializer.Serialize(badResult.Value);
        json.Should().Contain("tagihan terkunci");
        json.Should().Contain("BE001");
    }

    [Fact]
    public async Task Batal_WithoutHttpContext_WhenExecuted_ThenDefaultsHeadersAndReturnsOk()
    {
        // ARRANGE
        _sut.ControllerContext = new ControllerContext(); // HttpContext is null

        var req = new PakaiBedController.PakaiBedVoidRequest(
            PakaiBedId: "BE001",
            UserId: "USR003",
            VoidReason: "Batal admisi");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBedVoidCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var actionResult = await _sut.Batal(req);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedVoidCommand>(c =>
                c.PakaiBedId == "BE001" &&
                c.UserAgent == string.Empty &&
                c.RemoteIpAddress == "0.0.0.0"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveByBed_GivenExistingBed_WhenExecuted_ThenReturnsOkWithActiveOccupancy()
    {
        // ARRANGE
        const string bedId = "BD001";
        var expectedResponse = new PakaiBedActiveOccupancyResponse(
            PakaiBedId: "BE001",
            RegId: "RG001",
            PasienId: "PS001",
            PasienName: "Budi Santoso",
            BangsalId: "W001",
            BangsalName: "Bangsal Mawar",
            KamarId: "K001",
            KamarName: "Kamar 101",
            BedId: bedId,
            BedName: "Bed A",
            TglJamMasuk: new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
            BillingKelasId: "KL01",
            BillingKelasName: "Kelas 1",
            IsRoomingIn: false,
            MotherPakaiBedId: "");

        _mediatorMock
            .Setup(x => x.Send(It.Is<PakaiBedGetActiveByBedQuery>(q => q.BedId == bedId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.GetActiveByBed(bedId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedGetActiveByBedQuery>(q => q.BedId == bedId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveByBed_GivenEmptyBed_WhenExecuted_ThenReturnsOkWithNullData()
    {
        // ARRANGE
        const string bedId = "BD999";
        _mediatorMock
            .Setup(x => x.Send(It.Is<PakaiBedGetActiveByBedQuery>(q => q.BedId == bedId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PakaiBedActiveOccupancyResponse?)null);

        // ACT
        var actionResult = await _sut.GetActiveByBed(bedId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeNull();
    }

    [Fact]
    public async Task ListActiveByWard_GivenExistingWard_WhenExecuted_ThenReturnsOkWithActiveOccupancies()
    {
        // ARRANGE
        const string bangsalId = "W001";
        var expectedList = new List<PakaiBedActiveOccupancyResponse>
        {
            new(
                PakaiBedId: "BE001",
                RegId: "RG001",
                PasienId: "PS001",
                PasienName: "Budi Santoso",
                BangsalId: bangsalId,
                BangsalName: "Bangsal Mawar",
                KamarId: "K001",
                KamarName: "Kamar 101",
                BedId: "BD001",
                BedName: "Bed A",
                TglJamMasuk: new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
                BillingKelasId: "KL01",
                BillingKelasName: "Kelas 1",
                IsRoomingIn: false,
                MotherPakaiBedId: ""),
            new(
                PakaiBedId: "BE002",
                RegId: "RG002",
                PasienId: "PS002",
                PasienName: "Siti Rahma",
                BangsalId: bangsalId,
                BangsalName: "Bangsal Mawar",
                KamarId: "K002",
                KamarName: "Kamar 102",
                BedId: "BD002",
                BedName: "Bed B",
                TglJamMasuk: new DateTime(2026, 10, 16, 7, 0, 0, DateTimeKind.Utc),
                BillingKelasId: "KL02",
                BillingKelasName: "Kelas 2",
                IsRoomingIn: true,
                MotherPakaiBedId: "BE001")
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<PakaiBedListActiveByWardQuery>(q => q.BangsalId == bangsalId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // ACT
        var actionResult = await _sut.ListActiveByWard(bangsalId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedList);

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedListActiveByWardQuery>(q => q.BangsalId == bangsalId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListActiveByWard_GivenEmptyWard_WhenExecuted_ThenReturnsOkWithEmptyList()
    {
        // ARRANGE
        const string bangsalId = "W999";
        _mediatorMock
            .Setup(x => x.Send(It.Is<PakaiBedListActiveByWardQuery>(q => q.BangsalId == bangsalId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PakaiBedActiveOccupancyResponse>());

        // ACT
        var actionResult = await _sut.ListActiveByWard(bangsalId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        var list = jsend.data.Should().BeAssignableTo<IEnumerable<PakaiBedActiveOccupancyResponse>>().Subject;
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GetData_GivenPakaiBedId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string pakaiBedId = "BE001";
        var expectedResponse = new PakaiBedGetResponse(
            PakaiBedId: pakaiBedId,
            Reg: new RegReff("RG001", "PS001", "Budi"),
            Layanan: new LayananReff("LY001", "Rawat Inap"),
            Bed: new BedReff("BD001", "Bed 1", true),
            Kamar: new KamarReff("K001", "Kamar 101"),
            TipeKamar: new TipeKamarReff("TK01", "Standard", true),
            Tarif: 150000m,
            Periode: new PeriodePakaiBedType(
                new AuditInfoType("USR001", new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc)),
                AuditInfoType.Default));

        _mediatorMock
            .Setup(x => x.Send(It.Is<PakaiBedGetQuery>(q => q.PakaiBedId == pakaiBedId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var actionResult = await _sut.GetData(pakaiBedId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedGetQuery>(q => q.PakaiBedId == pakaiBedId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListDataByReg_GivenRegId_WhenExecuted_ThenSendsQueryAndReturnsOk()
    {
        // ARRANGE
        const string regId = "RG001";
        var expectedList = new List<PakaiBedListResponse>
        {
            new(
                PakaiBedId: "BE001",
                Reg: new RegReff(regId, "PS001", "Budi"),
                Layanan: new LayananReff("LY001", "Rawat Inap"),
                Bed: new BedReff("BD001", "Bed 1", true),
                Kamar: new KamarReff("K001", "Kamar 101"),
                TipeKamar: new TipeKamarReff("TK01", "Standard", true),
                Tarif: 150000m,
                Periode: new PeriodePakaiBedType(
                    new AuditInfoType("USR001", new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc)),
                    AuditInfoType.Default))
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<PakaiBedListByRegQuery>(q => q.RegId == regId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // ACT
        var actionResult = await _sut.ListDataByReg(regId);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedList);

        _mediatorMock.Verify(x => x.Send(
            It.Is<PakaiBedListByRegQuery>(q => q.RegId == regId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

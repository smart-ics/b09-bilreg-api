using System.Reflection;
using Bilreg.Api.Controllers.BedUsageContext;
using Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCockpitControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IWardCockpitEventPublisher> _publisherMock = new();
    private readonly WardCockpitController _sut;

    public WardCockpitControllerTest()
    {
        _sut = new WardCockpitController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_ShouldHaveExpectedAttributes()
    {
        var type = typeof(WardCockpitController);

        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/v1/ward-cockpit");

        var apiControllerAttr = type.GetCustomAttribute<ApiControllerAttribute>();
        apiControllerAttr.Should().NotBeNull();

        var authAttr = type.GetCustomAttribute<AuthorizeAttribute>();
        authAttr.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOverview_GivenBangsalId_SendsQueryAndReturnsJSendOk()
    {
        var expectedResponse = new WardCockpitOverviewDto(
            new WardCockpitBangsalDto("BGS-01", "Dahlia", "KLS-01", "Kelas 1"),
            new WardCockpitKpiDto(20, 15, 75.0, 3, 2, 0, 1),
            new List<WardCockpitRoomDto>(),
            new List<WardCockpitWaitingListDto>()
        );

        _mediatorMock
            .Setup(x => x.Send(It.Is<WardCockpitOverviewQuery>(q => q.BangsalId == "BGS-01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.GetOverview("BGS-01");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task GetBedDetail_GivenBedId_SendsQueryAndReturnsJSendOk()
    {
        var expectedResponse = new WardCockpitBedDetailDto(
            new WardCockpitBedDetailBedInfoDto("BED-01", "101-A", "Kamar 101", "Kelas 1"),
            null,
            new List<WardCockpitBedDetailTindakanDto>(),
            null,
            new List<WardCockpitBedDetailRiwayatMutasiDto>()
        );

        _mediatorMock
            .Setup(x => x.Send(It.Is<WardCockpitBedDetailQuery>(q => q.BedId == "BED-01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.GetBedDetail("BED-01");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task CheckIn_GivenCommand_SendsCommandAndReturnsJSendOk()
    {
        var cmd = new WardCheckInCommand("WL-01", "BED-01", "TK-01", "USR-01");
        var expectedResponse = new WardCheckInResult("PKB-01", "WL-01", "BED-01", "REG-01", "Placed");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.CheckIn(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task CheckIn_WithBangsalId_BroadcastingEvents()
    {
        var cmd = new WardCheckInCommand("WL-01", "BED-01", "TK-01", "USR-01");
        var expectedResponse = new WardCheckInResult("PKB-01", "WL-01", "BED-01", "REG-01", "Placed");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var actionResult = await controller.CheckIn(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync(
            "BGS-01", "BED-01", "Occupied", expectedResponse, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync(
            "BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task Transfer_GivenCommand_SendsCommandAndReturnsJSendOk()
    {
        var cmd = new PakaiBedTransferCommand("PKB-01", "BED-02", "USR-01", DateTime.UtcNow);
        var expectedResponse = new PakaiBedTransferResult("PKB-02", "PKB-01", "BED-02", cmd.TglJamTransfer);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.Transfer(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Transfer_WithBangsalId_BroadcastingBedStatusChanged()
    {
        var cmd = new PakaiBedTransferCommand("PKB-01", "BED-02", "USR-01", DateTime.UtcNow);
        var expectedResponse = new PakaiBedTransferResult("PKB-02", "PKB-01", "BED-02", cmd.TglJamTransfer);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var actionResult = await controller.Transfer(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync(
            "BGS-01", "BED-02", "Occupied", expectedResponse, default), Times.Once);
    }

    [Fact]
    public async Task Discharge_GivenCommand_SendsCommandAndReturnsJSendOk()
    {
        var cmd = new PakaiBedReleaseCommand("PKB-01", DateTime.UtcNow, "USR-01", "Checkout");
        var expectedResponse = new PakaiBedReleaseResult("PKB-01", cmd.TglJamOut, null);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.Discharge(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task RoomingIn_GivenCommand_SendsCommandAndReturnsJSendOk()
    {
        var cmd = new WardRoomingInCommand("PKB-MOM", "REG-BABY", "BED-01", "USR-01");
        var expectedResponse = new WardRoomingInResult("PKB-MOM", "REG-BABY", "BED-01", DateTime.UtcNow, "Active");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.RoomingIn(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task CompleteHk_GivenCommand_SendsCommandAndReturnsJSendOk()
    {
        var cmd = new BedOperationalVerifyReadyCommand("BED-01", "USR-HK");
        var expectedResponse = new BedOperationalVerifyReadyResult("BED-01", "Ready", "TRX-01", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.CompleteHk(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Maintenance_GivenCommand_SendsCommandAndReturnsJSendOk()
    {
        var cmd = new BedOperationalSetBlockerCommand("BED-01", "USR-ENG", "Perbaikan AC", BedRestrictionTypeEnum.Maintenance);
        var expectedResponse = new BedOperationalSetBlockerResult("BED-01", "Blocked", true, "TRX-02", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.Maintenance(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }
}

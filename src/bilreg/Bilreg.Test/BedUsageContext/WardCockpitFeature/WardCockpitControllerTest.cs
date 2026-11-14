using System.Reflection;
using Bilreg.Api.Controllers.BedUsageContext;
using Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.PatternHelper;
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
    public async Task Discharge_GivenBatalCheckIn_PublishesWaitingListUpdatedAndCleaningBedStatus()
    {
        var sut = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var cmd = new PakaiBedReleaseCommand("PKB-01", DateTime.UtcNow, "USR-01", "Batal Check-in: Salah nomor bed");
        var expectedResponse = new PakaiBedReleaseResult("PKB-01", cmd.TglJamOut, null, BangsalId: "BGS-01", BedId: "BED-01", IsBatalCheckIn: true);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await sut.Discharge(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task Discharge_GivenBatalCheckIn_WithDifferentRevertedWaitingListBangsalId_PublishesBothWards()
    {
        var sut = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var cmd = new PakaiBedReleaseCommand("PKB-01", DateTime.UtcNow, "USR-01", "Batal Check-in: Salah nomor bed");
        var expectedResponse = new PakaiBedReleaseResult("PKB-01", cmd.TglJamOut, null, BangsalId: "BGS-01", BedId: "BED-01", IsBatalCheckIn: true, RevertedWaitingListBangsalId: "BGS-11");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await sut.Discharge(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS-11", default), Times.Once);
    }

    [Fact]
    public async Task Discharge_GivenNormalDischarge_PublishesCleaningBedStatus()
    {
        var sut = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var cmd = new PakaiBedReleaseCommand("PKB-01", DateTime.UtcNow, "USR-01", "Pasien Pulang Sehat");
        var expectedResponse = new PakaiBedReleaseResult("PKB-01", cmd.TglJamOut, null, BangsalId: "BGS-01", BedId: "BED-01", IsBatalCheckIn: false);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await sut.Discharge(cmd);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync(It.IsAny<string>(), default), Times.Never);
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
    public async Task RoomingIn_WithBangsalIdAndNoBabyWaitingList_PublishesMotherWardWaitingListAndBedStatus()
    {
        var sut = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var cmd = new WardRoomingInCommand("PKB-MOM", "REG-BABY", "BED-01", "USR-01");
        var expectedResponse = new WardRoomingInResult("PKB-MOM", "REG-BABY", "BED-01", DateTime.UtcNow, "Active", BabyWaitingListBangsalId: null);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await sut.RoomingIn(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", expectedResponse, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
        _publisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RoomingIn_WithDifferentBabyWaitingListBangsalId_PublishesBothMotherAndBabyWardWaitingList()
    {
        var sut = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var cmd = new WardRoomingInCommand("PKB-MOM", "REG-BABY", "BED-01", "USR-01");
        var expectedResponse = new WardRoomingInResult("PKB-MOM", "REG-BABY", "BED-01", DateTime.UtcNow, "Active", BabyWaitingListBangsalId: "BGS-11");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await sut.RoomingIn(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", expectedResponse, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-11", default), Times.Once);
    }

    [Fact]
    public async Task RoomingIn_WithSameBabyWaitingListBangsalId_PublishesWaitingListOnlyOnceForThatWard()
    {
        var sut = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var cmd = new WardRoomingInCommand("PKB-MOM", "REG-BABY", "BED-01", "USR-01");
        var expectedResponse = new WardRoomingInResult("PKB-MOM", "REG-BABY", "BED-01", DateTime.UtcNow, "Active", BabyWaitingListBangsalId: "  bgs-01  ");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await sut.RoomingIn(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", expectedResponse, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
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

    [Fact]
    public async Task CheckIn_WithoutQueryBangsalId_DerivesFromCommandResultBangsalId_BroadcastingEvents()
    {
        var cmd = new WardCheckInCommand("WL-01", "BED-01", "TK-01", "USR-01");
        var expectedResponse = new WardCheckInResult("PKB-01", "WL-01", "BED-01", "REG-01", "Placed", BangsalId: "BGS-01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var actionResult = await controller.CheckIn(cmd, bangsalId: null);

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync(
            "BGS-01", "BED-01", "Occupied", expectedResponse, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync(
            "BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task CheckIn_WithoutQueryBangsalIdAndNullResultBangsalId_DerivesFromBedRepo_BroadcastingEvents()
    {
        var cmd = new WardCheckInCommand("WL-01", "BED-01", "TK-01", "USR-01");
        var expectedResponse = new WardCheckInResult("PKB-01", "WL-01", "BED-01", "REG-01", "Placed", BangsalId: null);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var bedRepoMock = new Mock<IBedRepo>();
        var bed = new BedType("BED-01", "Bed 01", new KamarReff("KMR-01", "Kamar 101"), new BangsalReff("BGS-02", "Bangsal Melati"), true);
        bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(bed));

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object, bedRepoMock.Object);
        var actionResult = await controller.CheckIn(cmd, bangsalId: null);

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync(
            "BGS-02", "BED-01", "Occupied", expectedResponse, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync(
            "BGS-02", default), Times.Once);
    }

    [Fact]
    public async Task CompleteHk_WithQueryBangsalId_BroadcastingBedStatusChangedAndWaitingListUpdated()
    {
        var cmd = new BedOperationalVerifyReadyCommand("BED-01", "USR-HK");
        var expectedResponse = new BedOperationalVerifyReadyResult("BED-01", "Ready", "TRX-01", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var actionResult = await controller.CompleteHk(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Ready", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task CompleteHk_WithoutQueryBangsalId_DerivesFromBedRepo_BroadcastingEvents()
    {
        var cmd = new BedOperationalVerifyReadyCommand("BED-01", "USR-HK");
        var expectedResponse = new BedOperationalVerifyReadyResult("BED-01", "Ready", "TRX-01", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var bedRepoMock = new Mock<IBedRepo>();
        var bed = new BedType("BED-01", "Bed 01", new KamarReff("KMR-01", "Kamar 101"), new BangsalReff("BGS-01", "Bangsal Dahlia"), true);
        bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(bed));

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object, bedRepoMock.Object);
        var actionResult = await controller.CompleteHk(cmd, bangsalId: null);

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Ready", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task Transfer_WithDifferentSourceAndTargetWards_BroadcastingWaitingListUpdatedToBothWards()
    {
        var cmd = new PakaiBedTransferCommand("PKB-01", "BED-02", "USR-01", DateTime.UtcNow);
        var expectedResponse = new PakaiBedTransferResult("PKB-02", "PKB-01", "BED-02", cmd.TglJamTransfer, SourceBedId: "BED-01", SourceBangsalId: "BGS-01", TargetBangsalId: "BGS-02");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var actionResult = await controller.Transfer(cmd);

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-02", "BED-02", "Occupied", expectedResponse, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-02", default), Times.Once);
    }

    [Fact]
    public async Task Maintenance_WithQueryBangsalId_WhenBlocked_BroadcastingBedStatusMaintenanceAndWaitingListUpdated()
    {
        var cmd = new BedOperationalSetBlockerCommand("BED-01", "USR-ENG", "Perbaikan AC", BedRestrictionTypeEnum.Maintenance, IsBlocked: true);
        var expectedResponse = new BedOperationalSetBlockerResult("BED-01", "Blocked", true, "TRX-02", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var actionResult = await controller.Maintenance(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Maintenance", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task Maintenance_WithQueryBangsalId_WhenUnblocked_BroadcastingBedStatusReadyAndWaitingListUpdated()
    {
        var cmd = new BedOperationalSetBlockerCommand("BED-01", "USR-ENG", "Perbaikan Selesai", BedRestrictionTypeEnum.Maintenance, IsBlocked: false);
        var expectedResponse = new BedOperationalSetBlockerResult("BED-01", "Ready", false, "TRX-03", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);
        var actionResult = await controller.Maintenance(cmd, bangsalId: "BGS-01");

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Ready", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task Maintenance_WithoutQueryBangsalId_DerivesFromBedRepo_BroadcastingEvents()
    {
        var cmd = new BedOperationalSetBlockerCommand("BED-01", "USR-ENG", "Perbaikan AC", BedRestrictionTypeEnum.Maintenance, IsBlocked: true);
        var expectedResponse = new BedOperationalSetBlockerResult("BED-01", "Blocked", true, "TRX-02", DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var bedRepoMock = new Mock<IBedRepo>();
        var bed = new BedType("BED-01", "Bed 01", new KamarReff("KMR-01", "Kamar 101"), new BangsalReff("BGS-01", "Bangsal Dahlia"), true);
        bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(bed));

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object, bedRepoMock.Object);
        var actionResult = await controller.Maintenance(cmd, bangsalId: null);

        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Maintenance", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
    }

    [Fact]
    public async Task Transfer_GivenMotherScope_WhenCleaningTriggered_BroadcastsCleaningToSourceBed()
    {
        // Arrange
        var cmd = new PakaiBedTransferCommand("PKB-01", "BED-02", "USR-01", DateTime.UtcNow, "Pindah Bed", null, null, "ibu", null);
        var expectedResponse = new PakaiBedTransferResult("PKB-02", "PKB-01", "BED-02", cmd.TglJamTransfer, SourceBedId: "BED-01", SourceBangsalId: "BGS-01", TargetBangsalId: "BGS-02", IsCleaningTriggered: true);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);

        // Act
        var actionResult = await controller.Transfer(cmd, bangsalId: "BGS-01");

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", null, default), Times.Never);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-02", "BED-02", "Occupied", expectedResponse, default), Times.Once);
    }

    [Fact]
    public async Task Transfer_GivenBabyScope_WhenCleaningNotTriggered_BroadcastsOccupiedToSourceBedAndDoesNotBroadcastCleaning()
    {
        // Arrange
        var cmd = new PakaiBedTransferCommand("PKB-01", "BED-BOX-01", "USR-01", DateTime.UtcNow, "Pindah Bayi Mandiri", null, null, "bayi", "REG-BABY");
        var expectedResponse = new PakaiBedTransferResult("PKB-02", "PKB-01", "BED-BOX-01", cmd.TglJamTransfer, SourceBedId: "BED-01", SourceBangsalId: "BGS-01", TargetBangsalId: "BGS-02", IsCleaningTriggered: false);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);

        // Act
        var actionResult = await controller.Transfer(cmd, bangsalId: "BGS-01");

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Never);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-02", "BED-BOX-01", "Occupied", expectedResponse, default), Times.Once);
    }

    [Fact]
    public async Task TransferToWaitingList_GivenMotherScope_WhenCleaningTriggered_BroadcastsCleaningToOldBedAndUpdatesWaitingLists()
    {
        // Arrange
        var cmd = new TransferToWardWaitingListCommand("PKB-01", "BGS-02", DateTime.UtcNow, "Transfer Bangsal Ibu & Bayi", "Catatan", "USR-01", null, "ibu", null);
        var expectedResponse = new TransferToWardWaitingListResult("WL-01", "PKB-01", "BED-01", "BGS-01", "BGS-02", cmd.TglJamTransfer, "Waiting", IsCleaningTriggered: true, BabyWaitingListId: "WL-01-B");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);

        // Act
        var actionResult = await controller.TransferToWaitingList(cmd, bangsalId: "BGS-01");

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", null, default), Times.Never);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-02", default), Times.Once);
    }

    [Fact]
    public async Task TransferToWaitingList_GivenBabyScope_WhenCleaningNotTriggered_BroadcastsOccupiedToOldBedAndDoesNotBroadcastCleaning()
    {
        // Arrange
        var cmd = new TransferToWardWaitingListCommand("PKB-01", "BGS-02", DateTime.UtcNow, "Transfer Bangsal Bayi Mandiri", "Catatan", "USR-01", null, "bayi", "REG-BABY");
        var expectedResponse = new TransferToWardWaitingListResult("WL-01", "PKB-01", "BED-01", "BGS-01", "BGS-02", cmd.TglJamTransfer, "Waiting", IsCleaningTriggered: false, BabyWaitingListId: "WL-01-B");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);

        // Act
        var actionResult = await controller.TransferToWaitingList(cmd, bangsalId: "BGS-01");

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Never);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-01", default), Times.Once);
        _publisherMock.Verify(p => p.PublishWaitingListUpdatedAsync("BGS-02", default), Times.Once);
    }

    [Fact]
    public async Task Discharge_GivenMotherScope_WhenCleaningTriggered_BroadcastsCleaningToBed()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("PKB-01", DateTime.UtcNow, "USR-01", "Pulang Ibu & Bayi", DischargeScope: "ibu");
        var expectedResponse = new PakaiBedReleaseResult("PKB-01", cmd.TglJamOut, null, BangsalId: "BGS-01", BedId: "BED-01", IsBatalCheckIn: false, IsCleaningTriggered: true);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);

        // Act
        var actionResult = await controller.Discharge(cmd);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", null, default), Times.Never);
    }

    [Fact]
    public async Task Discharge_GivenBabyScope_WhenCleaningNotTriggered_BroadcastsOccupiedToBedAndDoesNotBroadcastCleaning()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("PKB-01", DateTime.UtcNow, "USR-01", "Pulang Bayi Mandiri", DischargeScope: "bayi", BabyRegId: "REG-BABY");
        var expectedResponse = new PakaiBedReleaseResult("PKB-01", cmd.TglJamOut, null, BangsalId: "BGS-01", BedId: "BED-01", IsBatalCheckIn: false, IsCleaningTriggered: false);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new WardCockpitController(_mediatorMock.Object, _publisherMock.Object);

        // Act
        var actionResult = await controller.Discharge(cmd);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Cleaning", null, default), Times.Never);
        _publisherMock.Verify(p => p.PublishBedStatusChangedAsync("BGS-01", "BED-01", "Occupied", null, default), Times.Once);
    }

    [Fact]
    public async Task GetOverview_GivenBangsalWithDynamicTariffParameters_ReturnsJSendOkWithTariffFields()
    {
        var bangsal = new WardCockpitBangsalDto("BGS-01", "Dahlia", "KLS-01", "Kelas 1", LayananId: "RI001");
        var occupancy = new WardCockpitOccupancyDto(
            PakaiBedId: "PKB-01",
            RegId: "REG-01",
            MrNo: "MR-01",
            NamaPasien: "Pasien A",
            Gender: "L",
            Umur: "30 Thn",
            TglJamMasuk: DateTime.UtcNow,
            LosDays: 1,
            DpjpName: "dr. SpPD",
            PenjaminName: "BPJS",
            BillingKelasName: "Kelas 1",
            IsTitipKelas: false,
            IsRawatGabung: false,
            LayananId: "RI001",
            KelasId: "KL1",
            TipeTarifId: "KP"
        );
        var bed = new WardCockpitBedDto("BED-01", "101-A", "Occupied", occupancy, new WardCockpitOperationalStateDto("Ready", "None", null, null));
        var room = new WardCockpitRoomDto("ROOM-01", "Kamar 101", "Kelas 1", "Male", new[] { bed });
        var expectedResponse = new WardCockpitOverviewDto(
            bangsal,
            new WardCockpitKpiDto(1, 1, 100.0, 0, 0, 0, 0),
            new[] { room },
            new List<WardCockpitWaitingListDto>()
        );

        _mediatorMock
            .Setup(x => x.Send(It.Is<WardCockpitOverviewQuery>(q => q.BangsalId == "BGS-01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var actionResult = await _sut.GetOverview("BGS-01");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        var data = jsend.data.Should().BeOfType<WardCockpitOverviewDto>().Subject;
        data.Bangsal.LayananId.Should().Be("RI001");
        var returnedOcc = data.Rooms.Single().Beds.Single().CurrentOccupancy!;
        returnedOcc.LayananId.Should().Be("RI001");
        returnedOcc.KelasId.Should().Be("KL1");
        returnedOcc.TipeTarifId.Should().Be("KP");
    }

    [Fact]
    public async Task GetBedDetail_GivenBedWithDynamicTariffParameters_ReturnsJSendOkWithTariffFields()
    {
        var bedInfo = new WardCockpitBedDetailBedInfoDto("BED-01", "101-A", "Kamar 101", "Kelas 1", LayananId: "RI001", BangsalId: "BGS-01", KelasId: "KL1");
        var patient = new WardCockpitBedDetailPatientDto("MR-01", "Pasien A", "Laki-laki", "1990-01-01", "36 Tahun");
        var penjamin = new WardCockpitBedDetailPenjaminDto("BPJS", "BPJS Kesehatan", "000123");
        var dpjp = new WardCockpitBedDetailDpjpDto("DOK-01", "dr. Sutomo");
        var occupancy = new WardCockpitBedDetailOccupancyDto(
            PakaiBedId: "PKB-01",
            RegId: "REG-01",
            Patient: patient,
            Penjamin: penjamin,
            Dpjp: dpjp,
            CheckInTime: DateTime.UtcNow,
            LosDays: 1,
            BillingKelas: "Kelas 1",
            LayananId: "RI001",
            KelasId: "KL1",
            TipeTarifId: "KP"
        );
        var expectedResponse = new WardCockpitBedDetailDto(
            bedInfo,
            occupancy,
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
        var data = jsend.data.Should().BeOfType<WardCockpitBedDetailDto>().Subject;
        data.BedInfo.LayananId.Should().Be("RI001");
        data.BedInfo.BangsalId.Should().Be("BGS-01");
        data.BedInfo.KelasId.Should().Be("KL1");
        data.Occupancy.Should().NotBeNull();
        data.Occupancy!.LayananId.Should().Be("RI001");
        data.Occupancy.KelasId.Should().Be("KL1");
        data.Occupancy.TipeTarifId.Should().Be("KP");
    }
}

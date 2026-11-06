using System.Security.Claims;
using Bilreg.Api.Controllers.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IWardCockpitEventPublisher> _publisherMock = new();
    private readonly PakaiBedController _sut;

    public PakaiBedControllerTest()
    {
        _sut = new PakaiBedController(_mediatorMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task TransferToWaitingList_GivenValidRequest_ThenSendsCommandPublishesEventsAndReturnsOk()
    {
        // Arrange
        var req = new PakaiBedController.TransferToWardWaitingListRequest(
            PakaiBedId: "PKB001",
            TargetBangsalId: "BGS02",
            TglJamTransfer: new DateTime(2026, 11, 3, 10, 30, 0),
            AlasanTransfer: "Kebutuhan ICU",
            Catatan: "Catatan khusus",
            UserId: "nurse_1");

        var expectedResult = new TransferToWardWaitingListResult(
            WaitingListId: "WTL001",
            PakaiBedId: "PKB001",
            OldBedId: "BED01",
            OldBangsalId: "BGS01",
            TargetBangsalId: "BGS02",
            TglJamTransfer: req.TglJamTransfer,
            Status: "Waiting");

        _mediatorMock
            .Setup(x => x.Send(It.Is<TransferToWardWaitingListCommand>(c =>
                c.PakaiBedId == "PKB001" &&
                c.TargetBangsalId == "BGS02" &&
                c.AlasanTransfer == "Kebutuhan ICU" &&
                c.Catatan == "Catatan khusus" &&
                c.UserId == "nurse_1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _sut.TransferToWaitingList(req);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResult);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS01", "BED01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS02", default), Times.Once);
    }

    [Fact]
    public async Task TransferToWaitingList_WhenPublisherIsNull_FallbackToNullPublisherAndSucceeds()
    {
        // Arrange
        var sutWithoutPublisher = new PakaiBedController(_mediatorMock.Object, null);
        var req = new PakaiBedController.TransferToWardWaitingListRequest(
            PakaiBedId: "PKB001",
            TargetBangsalId: "BGS02",
            TglJamTransfer: new DateTime(2026, 11, 3, 10, 30, 0),
            AlasanTransfer: "Kebutuhan ICU");

        var expectedResult = new TransferToWardWaitingListResult(
            WaitingListId: "WTL001",
            PakaiBedId: "PKB001",
            OldBedId: "BED01",
            OldBangsalId: "BGS01",
            TargetBangsalId: "BGS02",
            TglJamTransfer: req.TglJamTransfer,
            Status: "Waiting");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TransferToWardWaitingListCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await sutWithoutPublisher.TransferToWaitingList(req);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResult);
    }

    [Fact]
    public async Task TransferToWaitingList_WhenUserIdEmpty_UsesClaimsPrincipalName()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, "user_from_claims")
        ], "mock"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var req = new PakaiBedController.TransferToWardWaitingListRequest(
            PakaiBedId: "PKB001",
            TargetBangsalId: "BGS02",
            TglJamTransfer: new DateTime(2026, 11, 3, 10, 30, 0),
            AlasanTransfer: "Kebutuhan ICU",
            Catatan: null,
            UserId: null);

        var expectedResult = new TransferToWardWaitingListResult(
            WaitingListId: "WTL001",
            PakaiBedId: "PKB001",
            OldBedId: "BED01",
            OldBangsalId: "BGS01",
            TargetBangsalId: "BGS02",
            TglJamTransfer: req.TglJamTransfer,
            Status: "Waiting");

        _mediatorMock
            .Setup(x => x.Send(It.Is<TransferToWardWaitingListCommand>(c =>
                c.PakaiBedId == "PKB001" &&
                c.UserId == "user_from_claims"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _sut.TransferToWaitingList(req);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResult);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS01", "BED01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS02", default), Times.Once);
    }

    [Fact]
    public async Task Release_GivenCommandWithBangsalAndBed_PublishesCleaningBedStatusAndReturnsOk()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("PKB001", DateTime.UtcNow, "nurse_1", "Pasien pulang");
        var expectedResponse = new PakaiBedReleaseResult("PKB001", cmd.TglJamOut, null, BangsalId: "BGS01", BedId: "BED01", IsBatalCheckIn: false);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var actionResult = await _sut.Release(cmd);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS01", "BED01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Release_GivenBatalCheckIn_PublishesCleaningAndWaitingListUpdated()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("PKB001", DateTime.UtcNow, "nurse_1", "Pembersihan Pasca Batal Check-In: Salah kamar");
        var expectedResponse = new PakaiBedReleaseResult("PKB001", cmd.TglJamOut, null, BangsalId: "BGS01", BedId: "BED01", IsBatalCheckIn: true);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var actionResult = await _sut.Release(cmd);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS01", "BED01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS01", default), Times.Once);
    }

    [Fact]
    public async Task Release_GivenBatalCheckIn_WithDifferentRevertedWaitingListBangsalId_PublishesBothWards()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("PKB001", DateTime.UtcNow, "nurse_1", "Pembersihan Pasca Batal Check-In: Salah kamar");
        var expectedResponse = new PakaiBedReleaseResult("PKB001", cmd.TglJamOut, null, BangsalId: "BGS01", BedId: "BED01", IsBatalCheckIn: true, RevertedWaitingListBangsalId: "BGS11");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var actionResult = await _sut.Release(cmd);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _publisherMock.Verify(x => x.PublishBedStatusChangedAsync("BGS01", "BED01", "Cleaning", null, default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS01", default), Times.Once);
        _publisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BGS11", default), Times.Once);
    }
}

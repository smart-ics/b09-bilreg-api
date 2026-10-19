using System.Security.Claims;
using Bilreg.Api.Controllers.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly RoomChargeController _sut;

    public RoomChargeControllerTest()
    {
        _sut = new RoomChargeController(_mediatorMock.Object);
    }

    [Fact]
    public async Task ProcessDailyCutoff_DelegatesToRoomChargePostDailyBatchCmd_ReturnsOkWithCounts()
    {
        // Arrange
        var req = new RoomChargePostDailyCutOffRequest("BANG01", new DateTime(2026, 10, 19), "user1");
        var batchResponse = new RoomChargeBatchResponse(5, 2, 0, new[] { "RC01", "RC02", "RC03", "RC04", "RC05" });

        RoomChargePostDailyBatchCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargePostDailyBatchCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeBatchResponse>, CancellationToken>((cmd, _) => capturedCmd = (RoomChargePostDailyBatchCmd)cmd)
            .ReturnsAsync(batchResponse);

        // Act
        var result = await _sut.ProcessDailyCutOff(req);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().NotBeNull();

        capturedCmd.Should().NotBeNull();
        capturedCmd!.BangsalId.Should().Be("BANG01");
        capturedCmd.CutOffDate.Should().Be(new DateTime(2026, 10, 19));
        capturedCmd.UserId.Should().Be("user1");
    }

    [Fact]
    public async Task ReconcileCheckout_DelegatesToRoomChargeReconcileCheckoutCmd_ReturnsOk()
    {
        // Arrange
        var checkOutTime = new DateTime(2026, 10, 19, 14, 30, 0);
        var req = new RoomChargeReconcileCheckoutRequest("PKB001", checkOutTime, "user1");
        var reconcileResponse = new RoomChargeReconcileResponse("RC001", 1.0m, 500000m);

        RoomChargeReconcileCheckoutCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeReconcileResponse>, CancellationToken>((cmd, _) => capturedCmd = (RoomChargeReconcileCheckoutCmd)cmd)
            .ReturnsAsync(reconcileResponse);

        // Act
        var result = await _sut.ReconcileCheckout(req);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(reconcileResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.PakaiBedId.Should().Be("PKB001");
        capturedCmd.CheckOutTime.Should().Be(checkOutTime);
        capturedCmd.UserId.Should().Be("user1");
    }

    [Fact]
    public async Task Reverse_DelegatesToRoomChargeReverseCmd_ReturnsOk()
    {
        // Arrange
        var req = new RoomChargeReverseRequest("Salah input kamar", "user1");
        var reverseResponse = new RoomChargeReverseResponse("RC001", "RC001-REV");

        RoomChargeReverseCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReverseCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeReverseResponse>, CancellationToken>((cmd, _) => capturedCmd = (RoomChargeReverseCmd)cmd)
            .ReturnsAsync(reverseResponse);

        // Act
        var result = await _sut.Reverse("RC001", req);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(reverseResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.RoomChargeId.Should().Be("RC001");
        capturedCmd.UserId.Should().Be("user1");
        capturedCmd.Reason.Should().Be("Salah input kamar");
    }

    [Fact]
    public async Task Reverse_ExtractsUserIdFromClaims_WhenNotProvidedInBody()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "claim_user_42") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var req = new RoomChargeReverseRequest("Storno pembatalan");
        var reverseResponse = new RoomChargeReverseResponse("RC001", "RC001-REV");

        RoomChargeReverseCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReverseCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeReverseResponse>, CancellationToken>((cmd, _) => capturedCmd = (RoomChargeReverseCmd)cmd)
            .ReturnsAsync(reverseResponse);

        // Act
        var result = await _sut.Reverse("RC001", req);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(reverseResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.UserId.Should().Be("claim_user_42");
    }

    [Fact]
    public async Task GetById_DelegatesToRoomChargeGetQuery_ReturnsOk()
    {
        // Arrange
        var detailView = new RoomChargeDetailView(
            "RC001", "PKB001", "REG001", "PAS001", "BANG01", "KAM01", "BED01",
            "2026-10-19", new DateTime(2026, 10, 19, 12, 0, 0), "DailyCutOff",
            "KL01", "KL01", "KL01", false, false, 0m, false, "",
            1.0m, 300000m, 0m, 300000m, "Posted", "BILL01", "", "",
            new List<RoomChargeKomponenView>());

        RoomChargeGetQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeGetQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeDetailView>, CancellationToken>((q, _) => capturedQuery = (RoomChargeGetQuery)q)
            .ReturnsAsync(detailView);

        // Act
        var result = await _sut.GetById("RC001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(detailView);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.RoomChargeId.Should().Be("RC001");
    }

    [Fact]
    public async Task ListByReg_DelegatesToRoomChargeListByRegQuery_ReturnsOk()
    {
        // Arrange
        var listView = new List<RoomChargeView>
        {
            new("RC001", "PKB001", "REG001", "PAS001", "BANG01", "KAM01", "BED01",
                "2026-10-18", new DateTime(2026, 10, 18, 12, 0, 0), "DailyCutOff",
                "KL01", "KL01", "KL01", false, false, 0m, false, "",
                1.0m, 300000m, 0m, 300000m, "Posted", "BILL01", "", ""),
            new("RC002", "PKB001", "REG001", "PAS001", "BANG01", "KAM01", "BED01",
                "2026-10-19", new DateTime(2026, 10, 19, 12, 0, 0), "DailyCutOff",
                "KL01", "KL01", "KL01", false, false, 0m, false, "",
                1.0m, 300000m, 0m, 300000m, "Posted", "BILL02", "", "")
        };

        RoomChargeListByRegQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeListByRegQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IEnumerable<RoomChargeView>>, CancellationToken>((q, _) => capturedQuery = (RoomChargeListByRegQuery)q)
            .ReturnsAsync(listView);

        // Act
        var result = await _sut.ListByReg("REG001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(listView);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.RegId.Should().Be("REG001");
    }

    [Fact]
    public async Task SummaryByReg_DelegatesToRoomChargeSummaryByRegQuery_ReturnsOk()
    {
        // Arrange
        var summaryView = new RoomChargeSummaryView(
            TotalChargedUnits: 2.0m,
            TotalAmount: 600000m,
            DifferentialAmount: 0m,
            TotalSubsidi: 0m,
            ActiveDaysCount: 2
        );

        RoomChargeSummaryByRegQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeSummaryByRegQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeSummaryView>, CancellationToken>((q, _) => capturedQuery = (RoomChargeSummaryByRegQuery)q)
            .ReturnsAsync(summaryView);

        // Act
        var result = await _sut.SummaryByReg("REG001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(summaryView);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.RegId.Should().Be("REG001");
    }
}

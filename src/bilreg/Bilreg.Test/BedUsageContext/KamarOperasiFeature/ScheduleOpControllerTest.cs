using System.Reflection;
using System.Security.Claims;
using Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ScheduleOpController _sut;

    public ScheduleOpControllerTest()
    {
        _sut = new ScheduleOpController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_ShouldHaveAuthorizeAttribute()
    {
        var authorizeAttr = typeof(ScheduleOpController).GetCustomAttribute<AuthorizeAttribute>(true);
        authorizeAttr.Should().NotBeNull("ScheduleOpController must be decorated with [Authorize]");
    }

    [Fact]
    public void SaveWorkbench_Action_ShouldHaveHttpPostAndRouteAttribute()
    {
        var method = typeof(ScheduleOpController).GetMethod(nameof(ScheduleOpController.SaveWorkbench));
        method.Should().NotBeNull();
        var httpPostAttr = method!.GetCustomAttribute<HttpPostAttribute>();
        httpPostAttr.Should().NotBeNull("SaveWorkbench action must be decorated with [HttpPost]");
        httpPostAttr!.Template.Should().Be("SaveWorkbench", "Route template must be 'SaveWorkbench'");
    }

    [Fact]
    public async Task SaveWorkbench_GivenValidCommand_WhenSuccess_Returns200OkWithJSendOk()
    {
        // ARRANGE
        var resources = new List<SaveWorkbenchResourceDto>
        {
            new("RES-01", "BLOOD", "PRC Gol A", 2, "Kolf", "Urgent")
        };

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01",
            DokterOperatorId: "DOC-01",
            ListPpaIds: ["DOC-01", "PPA-02"],
            TurnoverBuffer: 30,
            AlasanReschedule: "Alasan pindah jam",
            AlasanAssignLeader: "Alasan ganti dokter",
            IsOverride: false,
            OverrideReason: "",
            Publish: false,
            Resources: resources);

        var persistedResources = new List<OrderOpResourceDto>
        {
            OrderOpResourceDto.Create("RES-01", "ORD-001", "SCH-001", "BLOOD", "PRC Gol A", 2, "Kolf", "Urgent", "user-01", DateTime.UtcNow)
        };

        var expectedResponse = new OkScheduleOpSaveWorkbenchResponse(
            ScheduleOpId: "SCH-001",
            IsDraft: false,
            Rescheduled: true,
            LeaderChanged: true,
            PpaChanged: true,
            Published: false,
            ResourceChanged: true,
            Resources: persistedResources);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.SaveWorkbench(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveWorkbench_WhenUserClaimExists_OverridesUserIdWithServerClaim()
    {
        // ARRANGE
        const string claimUserId = "claim-workbench-user";
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("UserId", claimUserId)
        ], "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "untrusted-client-user");

        OkScheduleOpSaveWorkbenchCommand? capturedCmd = null;
        var expectedResponse = new OkScheduleOpSaveWorkbenchResponse(
            ScheduleOpId: "SCH-001",
            IsDraft: true,
            Rescheduled: false,
            LeaderChanged: false,
            PpaChanged: false,
            Published: false);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSaveWorkbenchCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<OkScheduleOpSaveWorkbenchResponse>, CancellationToken>((req, _) => capturedCmd = req as OkScheduleOpSaveWorkbenchCommand)
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.SaveWorkbench(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.UserId.Should().Be(claimUserId, "UserId must come from server authentication claims, not client payload");
    }

    [Fact]
    public async Task SaveWorkbench_WhenScheduleCollisionException_Returns409ConflictWithJSendFail()
    {
        // ARRANGE
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01");

        const string collisionMessage = "Bentrok jadwal kamar operasi: Kamar KMR-01 telah terisi.";

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSaveWorkbenchCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleCollisionException(collisionMessage));

        // ACT
        var result = await _sut.SaveWorkbench(cmd);

        // ASSERT
        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var jsend = conflict.Value.Should().BeOfType<JSendFail>().Subject;
        jsend.Status.Should().Be("fail");
        jsend.Data.Should().Be(collisionMessage);
    }

    [Fact]
    public async Task SaveWorkbench_WhenScheduleWarningException_Returns422UnprocessableEntityWithJSendFail()
    {
        // ARRANGE
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01");

        var warnings = new List<string>
        {
            "Dokter/PPA telah terjadwal pada operasi lain"
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSaveWorkbenchCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleWarningException(warnings));

        // ACT
        var result = await _sut.SaveWorkbench(cmd);

        // ASSERT
        var unprocessable = result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessable.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var jsend = unprocessable.Value.Should().BeOfType<JSendFail>().Subject;
        jsend.Status.Should().Be("fail");
        jsend.Data.Should().NotBeNull();

        var dataProp = jsend.Data.GetType().GetProperty("Warnings");
        dataProp.Should().NotBeNull();
        var actualWarnings = dataProp!.GetValue(jsend.Data) as IEnumerable<string>;
        actualWarnings.Should().BeEquivalentTo(warnings);

        var reqOverrideProp = jsend.Data.GetType().GetProperty("RequireOverride");
        reqOverrideProp.Should().NotBeNull();
        reqOverrideProp!.GetValue(jsend.Data).Should().Be(true);
    }

    [Fact]
    public async Task SaveWorkbench_WhenArgumentException_Returns400BadRequestWithJSendFail()
    {
        // ARRANGE
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01");

        const string errorMessage = "Alasan perubahan jadwal wajib diisi minimal 5 karakter.";

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSaveWorkbenchCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(errorMessage));

        // ACT
        var result = await _sut.SaveWorkbench(cmd);

        // ASSERT
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var jsend = badRequest.Value.Should().BeOfType<JSendFail>().Subject;
        jsend.Status.Should().Be("fail");
        jsend.Data.Should().Be(errorMessage);
    }

    [Fact]
    public async Task SaveWorkbench_WhenOwnershipViolation_Returns400BadRequestWithJSendFail()
    {
        // ARRANGE
        var cmd = new OkScheduleOpSaveWorkbenchCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01");

        const string errorMessage = "Cross-order resource mutation is forbidden: resource 'RES-01' does not belong to OrderOp 'ORD-001'";

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSaveWorkbenchCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // ACT
        var result = await _sut.SaveWorkbench(cmd);

        // ASSERT
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var jsend = badRequest.Value.Should().BeOfType<JSendFail>().Subject;
        jsend.Status.Should().Be("fail");
        jsend.Data.Should().Be(errorMessage);
    }
}

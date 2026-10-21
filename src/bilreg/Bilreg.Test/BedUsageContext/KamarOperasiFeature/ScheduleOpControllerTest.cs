using System.Reflection;
using System.Security.Claims;
using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
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
        // ASSERT
        var authorizeAttr = typeof(ScheduleOpController).GetCustomAttribute<AuthorizeAttribute>(true);
        authorizeAttr.Should().NotBeNull("ScheduleOpController must be decorated with [Authorize]");
    }

    [Fact]
    public async Task SetSchedule_GivenValidCommand_WhenSuccess_Returns200OkWithJSendOk()
    {
        // ARRANGE
        var cmd = new OkScheduleOpSetCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01",
            DokterOperatorId: "DOC-01",
            TurnoverBuffer: 30,
            AlasanPerubahan: "",
            IsOverride: false,
            OverrideReason: "");

        var expectedResponse = new OkScheduleOpSetResponse("SCH-001");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.SetSchedule(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetSchedule_WhenUserClaimExists_OverridesUserIdWithClaim()
    {
        // ARRANGE
        const string claimUserId = "claim-user-01";
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("UserId", claimUserId)
        ], "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var cmd = new OkScheduleOpSetCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "original-user");

        OkScheduleOpSetCommand? capturedCmd = null;
        var expectedResponse = new OkScheduleOpSetResponse("SCH-001");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSetCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<OkScheduleOpSetResponse>, CancellationToken>((req, _) => capturedCmd = req as OkScheduleOpSetCommand)
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.SetSchedule(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.UserId.Should().Be(claimUserId, "UserId must be forwarded from ClaimsPrincipal");
    }

    [Fact]
    public async Task SetSchedule_WhenRoomCollisionOccurs_Returns409ConflictWithJSendFail()
    {
        // ARRANGE
        var cmd = new OkScheduleOpSetCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01");

        const string collisionMessage = "Zero Room Collision: Ruangan KMR-01 telah terisi jadwal lain pada rentang waktu ini.";
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSetCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleCollisionException(collisionMessage));

        // ACT
        var result = await _sut.SetSchedule(cmd);

        // ASSERT
        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var jsend = conflict.Value.Should().BeOfType<JSendFail>().Subject;
        jsend.Status.Should().Be("fail");
        jsend.Data.Should().Be(collisionMessage);
    }

    [Fact]
    public async Task SetSchedule_WhenSoftWarningOccurs_Returns422UnprocessableEntityWithJSendFail()
    {
        // ARRANGE
        var cmd = new OkScheduleOpSetCommand(
            OrderOpId: "ORD-001",
            KamarId: "KMR-01",
            Tgl: "2026-10-20",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user-01");

        var warnings = new List<string>
        {
            "Jadwal Dokter Operator overlap di kamar lain",
            "Jeda turnover buffer kurang dari 30 menit"
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpSetCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleWarningException(warnings));

        // ACT
        var result = await _sut.SetSchedule(cmd);

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
    public async Task CancelSchedule_GivenValidCommand_WhenSuccess_Returns200OkWithJSendOk()
    {
        // ARRANGE
        var cmd = new OkScheduleOpCancelCommand(
            OrderOpId: "ORD-001",
            UserId: "user-01",
            AlasanBatal: "Pasien demam tinggi");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.CancelSchedule(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelSchedule_WhenUserClaimExists_OverridesUserIdWithClaim()
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

        var cmd = new OkScheduleOpCancelCommand("ORD-001", "original-user", "Alasan pembatalan klinis");
        OkScheduleOpCancelCommand? capturedCmd = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpCancelCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((req, _) => capturedCmd = req as OkScheduleOpCancelCommand)
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.CancelSchedule(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.UserId.Should().Be(claimUserId, "UserId must be forwarded from ClaimsPrincipal");
    }

    [Fact]
    public async Task CancelSchedule_GivenScheduleOpCancelRequest_WhenInvoked_ConvertsAndReturnsOk()
    {
        // ARRANGE
        var req = new ScheduleOpCancelRequest("ORD-001", "Alasan pembatalan via DTO");

        _mediatorMock
            .Setup(x => x.Send(It.Is<OkScheduleOpCancelCommand>(c => c.OrderOpId == req.OrderOpId && c.AlasanBatal == req.AlasanBatal), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.CancelSchedule(req);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(
            It.Is<OkScheduleOpCancelCommand>(c => c.OrderOpId == req.OrderOpId && c.AlasanBatal == req.AlasanBatal),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSchedule_GivenOrderOpId_WhenFound_Returns200OkWithFullDetailAndHistory()
    {
        // ARRANGE
        const string orderOpId = "ORD-001";
        var history = new List<OkScheduleOpHistDto>
        {
            new(
                NoUrut: 1,
                OldTglOp: new DateTime(2026, 10, 20, 8, 0, 0),
                NewTglOp: new DateTime(2026, 10, 20, 10, 0, 0),
                OldDurasi: 60,
                NewDurasi: 90,
                OldKamarId: "KMR-01",
                OldKamarName: "OK 1",
                NewKamarId: "KMR-02",
                NewKamarName: "OK 2",
                OldTeamLeadId: "DOC-01",
                OldTeamLeadName: "dr. Operator 1",
                NewTeamLeadId: "DOC-02",
                NewTeamLeadName: "dr. Operator 2",
                Alasan: "Dokter berhalangan di jam awal",
                IsOverride: true,
                OverrideReason: "Disetujui komite medik",
                UserId: "admin",
                CrtDate: new DateTime(2026, 10, 19, 14, 0, 0))
        };

        var response = new ScheduleOpGetResponse(
            OrderOpId: orderOpId,
            ScheduleOpId: "SCH-001",
            NamaOperasi: "Laparotomi",
            Urgency: "Elektif",
            PasienId: "PAS-01",
            PasienName: "Budi",
            RegId: "REG-01",
            TglOp: "2026-10-20",
            JamOp: "10:00",
            TglStartOp: "2026-10-20",
            JamStartOp: "10:00",
            TglFinishOp: "2026-10-20",
            JamFinishOp: "11:30",
            KamarId: "KMR-02",
            KamarName: "OK 2",
            DokterId: "DOC-02",
            DokterName: "dr. Operator 2",
            StatusOp: "Scheduled",
            ListPpa: new List<PpaReff>(),
            AlasanBatal: "",
            TurnoverBuffer: 30,
            History: history);

        _mediatorMock
            .Setup(x => x.Send(It.Is<OkScheduleOpGetQuery>(q => q.OrderOpId == orderOpId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // ACT
        var result = await _sut.GetSchedule(orderOpId);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(response);
        var actualResponse = jsend.data.Should().BeOfType<ScheduleOpGetResponse>().Subject;
        actualResponse.History.Should().HaveCount(1);
        actualResponse.TurnoverBuffer.Should().Be(30);

        _mediatorMock.Verify(x => x.Send(It.Is<OkScheduleOpGetQuery>(q => q.OrderOpId == orderOpId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListSchedule_GivenTgl_WhenInvoked_Returns200OkWithJSendOk()
    {
        // ARRANGE
        const string tgl = "2026-10-20";
        var expectedResponse = new List<OkListScheduleResponse>
        {
            new("PAS-01", "Budi", "1990-01-01", "ORD-001", "SCH-001", "Apendektomi", "Elective", 60, "08:00", "Scheduled", "DOC-01", "dr. Operator", "KMR-01", "OK 1")
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<OkListScheduleQuery>(q => q.TglYmd == tgl), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.ListSchedule(tgl);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task AddPpa_GivenValidCommand_WhenInvoked_Returns200Ok()
    {
        // ARRANGE
        var cmd = new OkScheduleOpAddPpaCommand("ORD-001", "PPA-01", "user-01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.AddPpa(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");
    }

    [Fact]
    public async Task AddPpa_WhenScheduleWarningExceptionThrown_Returns422UnprocessableEntityWithJSendFail()
    {
        // ARRANGE
        var cmd = new OkScheduleOpAddPpaCommand("ORD-001", "PPA-01", "user-01");
        var warnings = new List<string> { "Dokter/PPA PPA-01 sebagai Asisten telah terjadwal pada operasi lain" };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkScheduleOpAddPpaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ScheduleWarningException(warnings));

        // ACT
        var result = await _sut.AddPpa(cmd);

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
    public async Task AddPpa_WhenIsOverrideTrue_Returns200OkWithJSendOk()
    {
        // ARRANGE
        var cmd = new OkScheduleOpAddPpaCommand("ORD-001", "PPA-01", "user-01", IsOverride: true, OverrideReason: "Disetujui kepala ruangan");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.AddPpa(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePpa_GivenValidCommand_WhenInvoked_Returns200Ok()
    {
        // ARRANGE
        var cmd = new OkScheduleOpRemovePpaCommand("ORD-001", "PPA-01", "user-01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.RemovePpa(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");
    }

    [Fact]
    public async Task AssignLeader_GivenValidCommand_WhenInvoked_Returns200Ok()
    {
        // ARRANGE
        var cmd = new OkScheduleOpAssignLeaderCommand("ORD-001", "PPA-01", "user-01", "Perubahan operator utama");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _sut.AssignLeader(cmd);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(It.Is<OkScheduleOpAssignLeaderCommand>(c =>
            c.OrderOpId == "ORD-001" &&
            c.PpaId == "PPA-01" &&
            c.AlasanPerubahan == "Perubahan operator utama"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DependencyInjection_WhenAddInfrastructureCalled_RegistersScheduleOpHistDal()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // ACT
        services.AddInfrastructure(configuration);

        // ASSERT
        services.Should().Contain(d =>
            d.ServiceType == typeof(IScheduleOpHistDal) &&
            d.ImplementationType == typeof(ScheduleOpHistDal) &&
            d.Lifetime == ServiceLifetime.Scoped,
            "IScheduleOpHistDal must be registered as Scoped ScheduleOpHistDal in InfrastructureService");
    }

    [Fact]
    public void DependencyInjection_WhenAddApplicationCalled_RegistersPublishTrackerOnScheduleOpSetEvHandler()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // ACT
        services.AddApplication(configuration);

        // ASSERT
        services.Should().Contain(d =>
            d.ServiceType == typeof(INotificationHandler<OkScheduleOpSetEvent>) &&
            d.ImplementationType == typeof(PublishTrackerOnScheduleOpSetEvHandler),
            "PublishTrackerOnScheduleOpSetEvHandler must be registered as INotificationHandler<OkScheduleOpSetEvent> via MediatR");
    }
}

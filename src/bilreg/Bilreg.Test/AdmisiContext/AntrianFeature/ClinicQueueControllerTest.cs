using System.Reflection;
using System.Security.Claims;
using Bilreg.Api.Controllers.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.Shared;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class ClinicQueueControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUserContext> _currentUserMock = new();
    private readonly ClinicQueueController _sut;

    public ClinicQueueControllerTest()
    {
        _currentUserMock.Setup(u => u.GetActorUserId()).Returns("ACTOR-01");
        _sut = new ClinicQueueController(_mediatorMock.Object, _currentUserMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public void Controller_HasCorrectRouteAndApiControllerAttributes()
    {
        var type = typeof(ClinicQueueController);
        type.GetCustomAttributes(typeof(ApiControllerAttribute), true).Should().NotBeEmpty();
        var routeAttr = type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single();
        routeAttr.Template.Should().Be("api/v1/clinic-queue");
    }

    [Fact]
    public async Task Call_WhenInvoked_DispatchesClinicQueueCallCmdAndReturnsJSendOk()
    {
        var expectedResponse = new ClinicQueueOperationResponse("QUE-01", 5, "Waiting", 1);
        _mediatorMock.Setup(m => m.Send(It.IsAny<ClinicQueueCallCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var body = new ClinicQueueController.ClinicQueueCallBody("POLI-01", "DOC-01", "R-101", "USER-EXPLICIT");
        var result = await _sut.Call("QUE-01", 5, body, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ClinicQueueCallCmd>(c =>
                c.QueueCode == "QUE-01" &&
                c.NoUrut == 5 &&
                c.PoliId == "POLI-01" &&
                c.DokterId == "DOC-01" &&
                c.RuangId == "R-101" &&
                c.UserId == "USER-EXPLICIT"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Call_WhenUserIdOmitted_ResolvesFromCurrentUserContext()
    {
        var expectedResponse = new ClinicQueueOperationResponse("QUE-01", 5, "Waiting", 1);
        _mediatorMock.Setup(m => m.Send(It.IsAny<ClinicQueueCallCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var body = new ClinicQueueController.ClinicQueueCallBody("POLI-01", "DOC-01", "R-101", null);
        var result = await _sut.Call("QUE-01", 5, body, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();

        _mediatorMock.Verify(m => m.Send(
            It.Is<ClinicQueueCallCmd>(c => c.UserId == "ACTOR-01"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Recall_WhenInvoked_DispatchesClinicQueueRecallCmdAndReturnsJSendOk()
    {
        var expectedResponse = new ClinicQueueOperationResponse("QUE-01", 5, "Waiting", 2);
        _mediatorMock.Setup(m => m.Send(It.IsAny<ClinicQueueRecallCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var body = new ClinicQueueController.ClinicQueueRecallBody("POLI-01", "DOC-01", "R-101", "USER-EXPLICIT");
        var result = await _sut.Recall("QUE-01", 5, body, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ClinicQueueRecallCmd>(c =>
                c.QueueCode == "QUE-01" &&
                c.NoUrut == 5 &&
                c.PoliId == "POLI-01" &&
                c.DokterId == "DOC-01" &&
                c.RuangId == "R-101" &&
                c.UserId == "USER-EXPLICIT"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Skip_WhenInvoked_DispatchesClinicQueueSkipCmdAndReturnsJSendOk()
    {
        var expectedResponse = new ClinicQueueOperationResponse("QUE-01", 5, "Waiting", 1);
        _mediatorMock.Setup(m => m.Send(It.IsAny<ClinicQueueSkipCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var body = new ClinicQueueController.ClinicQueueSkipBody("POLI-01", "DOC-01", "USER-EXPLICIT");
        var result = await _sut.Skip("QUE-01", 5, body, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ClinicQueueSkipCmd>(c =>
                c.QueueCode == "QUE-01" &&
                c.NoUrut == 5 &&
                c.PoliId == "POLI-01" &&
                c.DokterId == "DOC-01" &&
                c.UserId == "USER-EXPLICIT"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CurrentDisplay_WhenInvoked_DispatchesClinicQueueGetDisplaySnapshotQueryAndReturnsJSendOk()
    {
        var snapshot = new ClinicDisplaySnapshotResponse(
            ActiveCalls: new[]
            {
                new ClinicQueueCallItem("QUE-01", 5, "A005", "Budi", "POLI-01", "Poli Penyakit Dalam",
                    "DOC-01", "dr. Sutomo", "R-101", "Ruang 101", 1, DateTime.Now)
            },
            WaitingItems: new[]
            {
                new ClinicQueueWaitingItem("QUE-01", 6, "A006", "Dewi", "POLI-01", "Poli Penyakit Dalam",
                    "DOC-01", "dr. Sutomo", DateTime.Now)
            });

        _mediatorMock.Setup(m => m.Send(It.IsAny<ClinicQueueGetDisplaySnapshotQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var result = await _sut.CurrentDisplay(
            displayId: "DISP-01",
            poliIds: new[] { "POLI-01", "POLI-02,POLI-03" },
            poliId: "POLI-04",
            tanggal: "2026-10-28",
            ct: CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(snapshot);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ClinicQueueGetDisplaySnapshotQuery>(q =>
                q.DisplayId == "DISP-01" &&
                q.Tanggal == "2026-10-28" &&
                q.PoliIds != null &&
                q.PoliIds.SequenceEqual(new[] { "POLI-01", "POLI-02", "POLI-03", "POLI-04" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CurrentDisplay_HasAllowAnonymousAttribute()
    {
        var method = typeof(ClinicQueueController).GetMethod(nameof(ClinicQueueController.CurrentDisplay));
        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().NotBeEmpty();
    }
}

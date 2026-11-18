using Bilreg.Api.Controllers.BillContext.TransportSub;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class AmbulanceControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AmbulanceController _sut;

    public AmbulanceControllerTest()
    {
        _sut = new AmbulanceController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Save_SendsCommandToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var cmd = new AmbulanceSaveCommand("AMB01", "Ambulans Alpha", "B 1234 CD", "Transport", "Available", 150000m);
        var expected = new AmbulanceSaveResponse("AMB01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Save(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddKomponen_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        var cmd = new AmbulanceAddKomponenCommand("AMB01", "KOMP01", 50000m, true);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AddKomponen(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveKomponen_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        var cmd = new AmbulanceRemoveKomponenCommand("AMB01", "KOMP01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RemoveKomponen(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddTujuan_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        var cmd = new AmbulanceAddTujuanCommand("AMB01", "TUJ01", 100000m);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AddTujuan(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveTujuan_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        var cmd = new AmbulanceRemoveTujuanCommand("AMB01", "TUJ01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RemoveTujuan(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.Is<AmbulanceActivateCommand>(c => c.AmbulanceId == "AMB01"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Activate("AMB01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(It.Is<AmbulanceActivateCommand>(c => c.AmbulanceId == "AMB01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.Is<AmbulanceDeactivateCommand>(c => c.AmbulanceId == "AMB01"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Deactivate("AMB01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(It.Is<AmbulanceDeactivateCommand>(c => c.AmbulanceId == "AMB01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetData_SendsQueryToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var expected = new AmbulanceGetResponse(
            "AMB01",
            "Ambulans Alpha",
            true,
            150000m,
            "B 1234 CD",
            "Transport",
            "Available",
            [],
            []);

        _mediatorMock
            .Setup(x => x.Send(It.Is<AmbulanceGetQuery>(q => q.AmbulanceId == "AMB01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetData("AMB01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(It.Is<AmbulanceGetQuery>(q => q.AmbulanceId == "AMB01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListData_SendsQueryToMediator_ReturnsOkWithList()
    {
        // Arrange
        var expected = new List<AmbulanceGetResponse>
        {
            new("AMB01", "Ambulans Alpha", true, 150000m, "B 1234 CD", "Transport", "Available", [], [])
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AmbulanceListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ListData();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expected);

        _mediatorMock.Verify(x => x.Send(It.IsAny<AmbulanceListQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

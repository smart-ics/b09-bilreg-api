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

public class TujuanTransportControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TujuanTransportController _sut;

    public TujuanTransportControllerTest()
    {
        _sut = new TujuanTransportController(_mediatorMock.Object)
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
        var cmd = new TujuanTransportSaveCommand("TUJ01", "Bandara", 250000m, false, "AMB01");
        var expected = new TujuanTransportSaveResponse("TUJ01");

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
    public async Task SetDefaultAmbulance_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        var cmd = new TujuanTransportSetDefaultAmbulanceCommand("TUJ01", "AMB01");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SetDefaultAmbulance(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnSetDefaultAmbulance_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.Is<TujuanTransportUnSetDefaultAmbulanceCommand>(c => c.TujuanTransportId == "TUJ01"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UnSetDefaultAmbulance("TUJ01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(It.Is<TujuanTransportUnSetDefaultAmbulanceCommand>(c => c.TujuanTransportId == "TUJ01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_SendsCommandToMediator_ReturnsOkDone()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.Is<TujuanTransportDeleteCommand>(c => c.TujuanTransportId == "TUJ01"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Delete("TUJ01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(It.Is<TujuanTransportDeleteCommand>(c => c.TujuanTransportId == "TUJ01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetData_SendsQueryToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var expected = new TujuanTransportGetResponse("TUJ01", "Bandara", 250000m, false, "AMB01");

        _mediatorMock
            .Setup(x => x.Send(It.Is<TujuanTransportGetQuery>(q => q.TujuanTransportId == "TUJ01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetData("TUJ01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(It.Is<TujuanTransportGetQuery>(q => q.TujuanTransportId == "TUJ01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListData_SendsQueryToMediator_ReturnsOkWithList()
    {
        // Arrange
        var expected = new List<TujuanTransportGetResponse>
        {
            new("TUJ01", "Bandara", 250000m, false, "AMB01")
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TujuanTransportListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ListData();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expected);

        _mediatorMock.Verify(x => x.Send(It.IsAny<TujuanTransportListQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

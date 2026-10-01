using Bilreg.Api.Controllers.ChargeContext;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TindakanController _sut;

    public TindakanControllerTest()
    {
        _sut = new TindakanController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Create_WhenSuccessful_ReturnsOkWithJSendOk()
    {
        // Arrange
        var cmd = new TdkCreateTindakanCmd("REG01", "LAY01", "TAR01", "USER01", []);
        var expectedResult = new TindakanCreateRespose("TDK001");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _sut.Create(cmd);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Create_WhenDomainThrowsArgumentException_ReturnsBadRequestWithJSendFailed()
    {
        // Arrange
        var cmd = new TdkCreateTindakanCmd("REG01", "LAY01", "TAR01", "USER01", []);
        var expectedExceptionMessage = "Komponen 'KOMP01' membutuhkan PPA";
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(expectedExceptionMessage));

        // Act
        var actionResult = await _sut.Create(cmd);

        // Assert
        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var jsend = badRequestResult.Value.Should().BeOfType<JSendFailed>().Subject;
        jsend.data.Should().Be(expectedExceptionMessage);
    }

    [Fact]
    public async Task Save_WhenSuccessful_ReturnsOkWithJSendOk()
    {
        // Arrange
        var cmd = new TdkSaveTindakanCmd("TDK01", "REG01", "LAY01", "TAR01", "USER01", []);
        var expectedResult = new TdkSaveTindakanRespose("TDK01");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var actionResult = await _sut.SaveTindakan(cmd);

        // Assert
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Save_WhenDomainThrowsArgumentException_ReturnsBadRequestWithJSendFailed()
    {
        // Arrange
        var cmd = new TdkSaveTindakanCmd("TDK01", "REG01", "LAY01", "TAR01", "USER01", []);
        var expectedExceptionMessage = "PPA tidak berlaku untuk komponen 'KOMP02'";
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(expectedExceptionMessage));

        // Act
        var actionResult = await _sut.SaveTindakan(cmd);

        // Assert
        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        var jsend = badRequestResult.Value.Should().BeOfType<JSendFailed>().Subject;
        jsend.data.Should().Be(expectedExceptionMessage);
    }
}

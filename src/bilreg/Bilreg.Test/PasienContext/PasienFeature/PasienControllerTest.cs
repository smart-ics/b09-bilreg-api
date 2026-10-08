using Bilreg.Api.Controllers.PasienContext;
using Bilreg.Application.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly PasienController _controller;

    public PasienControllerTest()
    {
        _controller = new PasienController(_mediatorMock.Object);
    }

    [Fact]
    public async Task UT01_GivenApproveUploadSasetCmd_WhenApproveUploadSaset_ThenSendsToMediatorAndReturnsOkDone()
    {
        // Arrange
        var cmd = new PasienApproveUploadSasetCmd("00100123456");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ApproveUploadSaset(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenMediatorThrows_WhenApproveUploadSaset_ThenPropagatesException()
    {
        // Arrange
        var cmd = new PasienApproveUploadSasetCmd("00100123456");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Pasien 00100123456 not found"));

        // Act
        var act = async () => await _controller.ApproveUploadSaset(cmd);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pasien 00100123456 not found");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }
}

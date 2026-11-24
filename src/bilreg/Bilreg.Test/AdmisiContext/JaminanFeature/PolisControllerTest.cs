using Bilreg.Api.Controllers.AdmisiContext.JaminanSub;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class PolisControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly PolisController _sut;

    public PolisControllerTest()
    {
        _sut = new PolisController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Update_WhenCalled_SendsCommandToMediatorAndReturnsOkWithJSend()
    {
        // Arrange
        var command = new PolisUpdateCommand(
            PolisId: "POL001",
            PasienId: "PAS001",
            NoPolis: "NP001",
            AtasName: "Budi",
            ExpiredDate: "2027-12-31",
            KelasRanapId: "KL01",
            StatusPesertaId: "P",
            IsCoverRajal: true
        );
        var expectedResponse = new PolisUpdateResponse("POL001");

        _mediatorMock
            .Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Update(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }
}

using Bilreg.Api.Controllers.PasienContext;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
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

    [Fact]
    public async Task UT03_GivenPasienSetKtpCommand_WhenSetDataKtp_ThenSendsToMediatorAndReturnsOk()
    {
        // Arrange
        var cmd = new PasienSetKtpCommand("00100123456", "Budi", "3578010101900001", "Jl. Mawar", "O", "L", "Sby", "1990-05-20", "Islam", "Kawin", "01", "02", "KEL-01", false)
        {
            UserId = "user-test"
        };
        var expectedResponse = new PasienSetKtpResponse(
            "00100123456", false, false,
            new PasienSetKtpResponsePerson("Budi", "L", "1990-05-20", "O", "Sby"),
            new PasienSetKtpResponsePerson("Budi", "L", "1990-05-20", "O", "Sby"));
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SetDataKtp(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT04_GivenPasienAddContactCommand_WhenAddContact_ThenSendsToMediatorAndReturnsOkDone()
    {
        // Arrange
        var cmd = new PasienAddContactCommand("00100123456", 1, "08123456789")
        {
            UserId = "user-test"
        };
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddContact(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT05_GivenPasienSetDemografiCommand_WhenSetStatusSosial_ThenSendsToMediatorAndReturnsOkDone()
    {
        // Arrange
        var cmd = new PasienSetDemografiCommand(
            "00100123456", "SK-01", "AG-01", "SKU-01", "PK-01", "PD-01",
            "Jl. A", "B", "C", "KEL-01", "Sby", "60111", "3578", "a@b.com", "081",
            "Dewi", "Istri", "082", "Jl. A", "B", "Sby", "60111", "Ibu")
        {
            UserId = "user-test"
        };
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SetStatusSosial(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT06_GivenPasienNonActiveCmd_WhenNonActive_ThenSendsToMediatorAndReturnsOkDone()
    {
        // Arrange
        var cmd = new PasienNonActiveCmd("00100123456")
        {
            UserId = "user-test"
        };
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.NonActive(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT07_GivenPasienReActiveCmd_WhenReActive_ThenSendsToMediatorAndReturnsOkDone()
    {
        // Arrange
        var cmd = new PasienReActiveCmd("00100123456")
        {
            UserId = "user-test"
        };
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ReActive(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT08_GivenPasienCreateCommand_WhenCreate_ThenSendsToMediatorAndReturnsOk()
    {
        // Arrange
        var cmd = new PasienCreateCommand(
            "Agus Budiono", "Surabaya", "1990-05-15",
            "L", "Agus", "Siti Aminah", "O",
            "Jl. Basuki Rahmat", "RT 01", "Genteng",
            "Surabaya", "60271", "081234567890", "3578011505900001");
        var expectedResponse = new PasienCreateResponse("00100123456");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Create(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT09_GivenPasienCreateByKtpCommand_WhenCreateByKtp_ThenSendsToMediatorAndReturnsOk()
    {
        // Arrange
        var cmd = new PasienCreateByKtpCommand(
            "Dewi Sartika", "3578015010920002", "Jl. Pemuda No. 45", "A",
            "P", "Surabaya", "1992-10-10", "Islam", "Kawin",
            "Surabaya", "60271", "03", "04", "0010010101");
        var expectedResponse = new PasienCreateByKtpResponse("00100123456");
        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateByKtp(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT10_GivenPasienId_WhenGetData_ThenSendsToMediatorAndReturnsOk()
    {
        // Arrange
        var pasienId = "00100123456";
        var expectedResponse = new PasienGetResponse(
            pasienId, "00100123456", "Agus Budiono", "Surabaya", "1990-05-15", "34 th",
            "Agus", "L", "Siti Aminah", "O", "agus@example.com", "081234567890", true,
            AlamatType.Default, KelurahanType.Default, KtpType.Default, IdentitasType.Default,
            PasienKeluargaType.Default, StatusKawinDkType.Default, AgamaType.Default, SukuType.Default,
            PekerjaanDkType.Default, PendidikanDkType.Default,
            new PasienSasetResponse(string.Empty, false, string.Empty, string.Empty, false, string.Empty, string.Empty));
        _mediatorMock
            .Setup(x => x.Send(It.Is<PasienGetQuery>(q => q.PasienId == pasienId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetData(pasienId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
        _mediatorMock.Verify(x => x.Send(It.Is<PasienGetQuery>(q => q.PasienId == pasienId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UT11_GivenKeyword_WhenSearch_ThenSendsToMediatorAndReturnsOk()
    {
        // Arrange
        var keyword = "Agus";
        var expectedList = new List<PasienSearchResponse>
        {
            new("00100123456", true, PersonInfoType.Default)
        };
        _mediatorMock
            .Setup(x => x.Send(It.Is<PasienSearchQuery>(q => q.Keyword == keyword), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // Act
        var result = await _controller.Search(keyword);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedList);
        _mediatorMock.Verify(x => x.Send(It.Is<PasienSearchQuery>(q => q.Keyword == keyword), It.IsAny<CancellationToken>()), Times.Once);
    }
}

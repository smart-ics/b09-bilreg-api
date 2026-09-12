using Bilreg.Api.Controllers.PasienContext.DigitalSignFeature;
using Bilreg.Application.PasienContext.DigitalSignFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using System.Text.Json;
using Xunit;

namespace Bilreg.Test.PasienContext.DigitalSignFeature;

public class DigitalSignControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DigitalSignController _controller;

    public DigitalSignControllerTest()
    {
        _controller = new DigitalSignController(_mediatorMock.Object);
    }

    [Fact]
    public async Task UT01_GivenNotFound_WhenResolvePatientSigner_ThenReturns404WithNestedPatient()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ResolvePatientSignerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvePatientSignerResponse(
                HiDokPatientSignerResolveStatus.NotFound, "", "", "Pasien tidak ditemukan",
                new ResolvePatientSignerPatientInfo(
                    "", "317304001590223", "GABRIELLA SIFA", "KAPAL BTN SOSIAL BLOK C/31",
                    "18-05-2002", "085312345678", "3171045105020003", "RSHSD")));

        var actionResult = await _controller.ResolvePatientSigner("317304001590223");

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);

        var jsend = objectResult.Value.Should().BeOfType<JSendModel>().Subject;
        jsend.status.Should().Be("failed");
        jsend.code.Should().Be("404");

        var json = JsonSerializer.Serialize(jsend);
        json.Should().Contain("\"message\":\"Pasien tidak ditemukan\"");
        json.Should().Contain("\"patient\":{");
        json.Should().Contain("\"UserrID\":\"\"");
        json.Should().Contain("\"NoMR\":\"317304001590223\"");
        json.Should().Contain("\"PasienName\":\"GABRIELLA SIFA\"");
        json.Should().Contain("\"Alamat\":\"KAPAL BTN SOSIAL BLOK C/31\"");
        json.Should().Contain("\"TglLahir\":\"18-05-2002\"");
        json.Should().Contain("\"NoTelp\":\"085312345678\"");
        json.Should().Contain("\"NoKTP\":\"3171045105020003\"");
        json.Should().Contain("\"RSID\":\"RSHSD\"");
    }

    [Fact]
    public async Task UT02_GivenNotFoundWithoutLocalPasien_WhenResolvePatientSigner_ThenMessageOnly()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ResolvePatientSignerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvePatientSignerResponse(
                HiDokPatientSignerResolveStatus.NotFound, "", "", "Pasien tidak ditemukan", null));

        var actionResult = await _controller.ResolvePatientSigner("317304001590223");

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);

        var jsend = objectResult.Value.Should().BeOfType<JSendModel>().Subject;
        jsend.status.Should().Be("failed");
        jsend.code.Should().Be("404");

        var json = JsonSerializer.Serialize(jsend);
        json.Should().Contain("\"message\":\"Pasien tidak ditemukan\"");
        json.Should().NotContain("\"patient\"");
    }

    [Fact]
    public async Task UT03_GivenNotVerified_WhenResolvePatientSigner_ThenReturns428()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ResolvePatientSignerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvePatientSignerResponse(
                HiDokPatientSignerResolveStatus.NotVerified, "", "", "Signer belum terverifikasi",
                new ResolvePatientSignerPatientInfo(
                    "", "317304001590223", "GABRIELLA SIFA", "-",
                    "18-05-2002", "-", "-", "RSHSD")));

        var actionResult = await _controller.ResolvePatientSigner("317304001590223");

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(428);
        var jsend = objectResult.Value.Should().BeOfType<JSendModel>().Subject;
        jsend.status.Should().Be("failed");
        jsend.code.Should().Be("428");
        JsonSerializer.Serialize(jsend).Should().Contain("\"patient\":{");
    }

    [Fact]
    public async Task UT04_GivenProvisionFailed_WhenResolvePatientSigner_ThenReturns502()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ResolvePatientSignerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvePatientSignerResponse(
                HiDokPatientSignerResolveStatus.ProvisionFailed, "", "", "Gagal provisi signer",
                new ResolvePatientSignerPatientInfo(
                    "", "317304001590223", "GABRIELLA SIFA", "-",
                    "18-05-2002", "-", "-", "RSHSD")));

        var actionResult = await _controller.ResolvePatientSigner("317304001590223");

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(502);
        var jsend = objectResult.Value.Should().BeOfType<JSendModel>().Subject;
        jsend.code.Should().Be("502");
    }

    [Fact]
    public async Task UT05_GivenSuccess_WhenResolvePatientSigner_ThenReturnsOkEnvelope()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ResolvePatientSignerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvePatientSignerResponse(
                HiDokPatientSignerResolveStatus.Success, "U-123", "SIG-456", "", null));

        var actionResult = await _controller.ResolvePatientSigner("317304001590223");

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        json.Should().Contain("\"UserrId\":\"U-123\"");
        json.Should().Contain("\"SignerId\":\"SIG-456\"");
        json.Should().NotContain("\"patient\"");
    }
}
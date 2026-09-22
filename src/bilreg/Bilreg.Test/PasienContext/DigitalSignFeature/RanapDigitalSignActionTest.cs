using Bilreg.Api.Controllers.PasienContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Test.PasienContext.DigitalSignFeature;

public class RanapDigitalSignActionTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DigitalSignController _controller;

    public RanapDigitalSignActionTest()
    {
        _controller = new DigitalSignController(_mediatorMock.Object);
    }

    private static AdmRecordDigitalSignBody SampleBody() =>
        new("RG00000001", "RG00000001", "HIS-DOC-2026-001",
            Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"),
            "file.pdf", "user1");

    [Fact]
    public async Task GivenValidBody_WhenRecord_ThenReturnsOk()
    {
        var signingId = Guid.NewGuid().ToString("D");
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmRecordDigitalSignCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmRecordDigitalSignResponse(signingId));

        var result = await _controller.RecordRanapDigitalSign(SampleBody() with { SigningRequestId = signingId });

        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(x => x.Send(
            It.Is<AdmRecordDigitalSignCmd>(c => c.RegId == "RG00000001" && c.HisReference == "RG00000001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenMissingAdmission_WhenRecord_ThenReturns404()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmRecordDigitalSignCmd>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Admission 'RG00000001' tidak ditemukan."));

        var result = await _controller.RecordRanapDigitalSign(SampleBody());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GivenBlankRegId_WhenGet_ThenReturns400()
    {
        var result = await _controller.GetRanapDigitalSign(" ", "HIS-DOC-2026-001");

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<JSendFailed>();
        _mediatorMock.Verify(x => x.Send(
            It.IsAny<AdmGetDigitalSignQry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenBlankDokumenId_WhenGet_ThenReturns400()
    {
        var result = await _controller.GetRanapDigitalSign("RG00000001", " ");

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(
            It.IsAny<AdmGetDigitalSignQry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenDocExists_WhenGetSingle_ThenReturnsOk()
    {
        var item = new AdmGetDigitalSignResponse(
            Guid.NewGuid().ToString("D"), "RG00000001", "HIS-ENC-999",
            "HIS-DOC-2026-001", "file.pdf");
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmGetDigitalSignQry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmGetDigitalSignListResponse([item]));

        var result = await _controller.GetRanapDigitalSign("RG00000001", "HIS-DOC-2026-001");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GivenUnknownDoc_WhenGetSingle_ThenReturns404()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmGetDigitalSignQry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmGetDigitalSignListResponse([]));

        var result = await _controller.GetRanapDigitalSign("RG00000001", "HIS-DOC-UNKNOWN");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GivenRegId_WhenList_ThenReturnsOkArray()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmGetDigitalSignQry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmGetDigitalSignListResponse([]));

        var result = await _controller.ListRanapDigitalSign("RG00000001");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<JSendOk>();
        _mediatorMock.Verify(x => x.Send(
            It.Is<AdmGetDigitalSignQry>(q => q.RegId == "RG00000001" && q.DokumenId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenBlankRegId_WhenList_ThenReturns400()
    {
        var result = await _controller.ListRanapDigitalSign(" ");

        result.Should().BeOfType<BadRequestObjectResult>();
        _mediatorMock.Verify(x => x.Send(
            It.IsAny<AdmGetDigitalSignQry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

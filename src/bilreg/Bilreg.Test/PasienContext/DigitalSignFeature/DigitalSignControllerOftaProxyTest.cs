using System.IO;
using System.Text;
using Bilreg.Api.Controllers.PasienContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.DigitalSignFeature;

public class DigitalSignControllerOftaProxyTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DigitalSignController _controller;

    public DigitalSignControllerOftaProxyTest()
    {
        _controller = new DigitalSignController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GivenNoFile_WhenProcessGeneralConsentOftaProxy_ThenReturns400()
    {
        var form = new AdmGeneralConsentOftaProxyForm
        {
            RegId = "RG001",
            DokumenId = "GC001",
            ExternalDocumentId = "EXT-001",
            OfficerRef = "OFF-001",
            SignPositionDesc = "Petugas",
            File = null!
        };

        var result = await _controller.ProcessGeneralConsentOftaProxy(form);

        var objectResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GivenValidForm_WhenProcessGeneralConsentOftaProxy_ThenSendsCmdAndReturns200()
    {
        var content = "PDF content test";
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var fileMock = new FormFile(stream, 0, bytes.Length, "file", "consent.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var form = new AdmGeneralConsentOftaProxyForm
        {
            RegId = "RG001",
            DokumenId = "GC001",
            ExternalDocumentId = "EXT-001",
            OfficerRef = "OFF-001",
            SignPositionDesc = "Petugas",
            Passphrase = "pass",
            Otp = "123456",
            File = fileMock
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AdmGeneralConsentOftaProxyCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdmGeneralConsentOftaProxyResponse(
                DocId: "DOC-123",
                DocState: "COMPLETED",
                SignState: "SIGNED",
                SignedDocUrl: "http://ofta/signed.pdf",
                OfficerEmail: "officer@mail.com",
                OfficerName: "Petugas",
                SignedDate: DateTime.Now,
                SigningRequestId: Guid.NewGuid().ToString("D"),
                RegId: "RG001",
                DokumenId: "GC001",
                ExternalDocumentId: "EXT-001",
                IsAlreadySigned: false,
                SignedPdfBase64: Convert.ToBase64String(bytes)));

        var result = await _controller.ProcessGeneralConsentOftaProxy(form);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
    }
}

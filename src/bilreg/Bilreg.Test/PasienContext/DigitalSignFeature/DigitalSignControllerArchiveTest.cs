using System.Net;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;
using FluentAssertions;

namespace Bilreg.Test.PasienContext.DigitalSignFeature;

public class DigitalSignControllerArchiveTest
{
    private readonly Mock<IMediator> _mediator = new();

    [Fact]
    public async Task TriggerGeneralConsentArchive_ValidRequest_ReturnsOk()
    {
        var expectedResponse = new GeneralConsentArchiveBatchResult(
            ProcessedCount: 1,
            SucceededCount: 1,
            FailedCount: 0,
            SkippedCount: 0,
            Details: [new GeneralConsentArchiveResult(
                SigningRequestId: "b14e668c-ff5d-4952-b80c-0d35ee414777",
                RegId: "REG-01",
                DokumenId: "DOC-01",
                OftaDocId: "OFTA-01",
                Success: true,
                IsAlreadyArchived: false,
                ArchiveId: "ARCH-123")]);

        _mediator.Setup(m => m.Send(It.IsAny<GeneralConsentArchiveTriggerCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var controller = new Bilreg.Api.Controllers.PasienContext.DigitalSignFeature.DigitalSignController(_mediator.Object);

        var req = new Bilreg.Api.Controllers.PasienContext.DigitalSignFeature.GeneralConsentArchiveTriggerRequest
        {
            SigningRequestId = "b14e668c-ff5d-4952-b80c-0d35ee414777",
            UserId = "user-test"
        };

        var result = await controller.TriggerGeneralConsentArchive(req);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);
    }
}

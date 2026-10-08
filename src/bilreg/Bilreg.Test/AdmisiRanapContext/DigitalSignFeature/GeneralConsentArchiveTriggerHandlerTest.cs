using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class GeneralConsentArchiveTriggerHandlerTest
{
    private readonly Mock<IRanapDigitalSignRepo> _repoMock = new();
    private readonly Mock<IOftaGeneralConsentClient> _oftaClientMock = new();
    private readonly Mock<IPatientSignedDocumentRetrievalService> _retrievalMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamMock = new();
    private readonly Mock<ILogger<GeneralConsentArchiveWorker>> _loggerMock = new();

    private GeneralConsentArchiveWorker CreateWorker() => new(
        _repoMock.Object,
        _oftaClientMock.Object,
        _retrievalMock.Object,
        _auditRepoMock.Object,
        _tglJamMock.Object,
        _loggerMock.Object);

    [Fact]
    public async Task Handle_BySigningRequestId_InvokesProcessOne()
    {
        var model = RanapDigitalSignModel.CatatOftaProxy(
            regId: "REG-100",
            hisReference: "REG-100",
            dokumenId: "DOC-100",
            signingRequestId: "b14e668c-ff5d-4952-b80c-0d35ee414777",
            pasien: new PasienReff("PSN-100", "Budi", new DateOnly(1990, 1, 1), "L"),
            signerId: "SGN-100",
            fileName: "general-consent.pdf",
            oftaDocId: "OFTA-DOC-100",
            oftaDocState: "Uploaded",
            oftaSignState: "Signed",
            officerRef: "officer@hospital.com",
            officerEmail: "officer@hospital.com",
            officerName: "Officer Satu",
            externalDocumentId: "EXT-100",
            signedDocUrl: "http://ofta/signed/100.pdf",
            auditUserId: "user1",
            createdAt: new DateTime(2026, 10, 8, 10, 0, 0),
            patientSignState: "Signed");

        _repoMock.Setup(x => x.LoadEntity(It.Is<IRanapDigitalSignKey>(k => k.SigningRequestId == model.SigningRequestId)))
            .Returns(MayBe.From(model));

        var fakePdfBytes = "%PDF-1.4"u8.ToArray();
        _retrievalMock.Setup(x => x.RetrievePatientSignedPdfAsync(model.SigningRequestId, model.SignedDocUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakePdfBytes);

        _oftaClientMock.Setup(x => x.ArchiveDocAsync(
                model.OftaDocId, fakePdfBytes, model.FileName, model.RegId, model.DokumenId, model.ExternalDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ARCH-100");

        var worker = CreateWorker();
        var handler = new GeneralConsentArchiveTriggerHandler(worker);

        var cmd = new GeneralConsentArchiveTriggerCmd(
            SigningRequestId: model.SigningRequestId,
            TriggerType: "ON_DEMAND",
            UserId: "user1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.ProcessedCount.Should().Be(1);
        result.SucceededCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        result.Details.First().ArchiveId.Should().Be("ARCH-100");
    }

    [Fact]
    public async Task Handle_ByRegAndDokumenId_InvokesProcessCorrelation()
    {
        var model = RanapDigitalSignModel.CatatOftaProxy(
            regId: "REG-200",
            hisReference: "REG-200",
            dokumenId: "DOC-200",
            signingRequestId: "b14e668c-ff5d-4952-b80c-0d35ee414888",
            pasien: new PasienReff("PSN-200", "Siti", new DateOnly(1992, 2, 2), "P"),
            signerId: "SGN-200",
            fileName: "general-consent.pdf",
            oftaDocId: "OFTA-DOC-200",
            oftaDocState: "Uploaded",
            oftaSignState: "Signed",
            officerRef: "officer@hospital.com",
            officerEmail: "officer@hospital.com",
            officerName: "Officer Satu",
            externalDocumentId: "EXT-200",
            signedDocUrl: "http://ofta/signed/200.pdf",
            auditUserId: "user1",
            createdAt: new DateTime(2026, 10, 8, 10, 0, 0),
            patientSignState: "Signed");

        _repoMock.Setup(x => x.LoadByRegDokumen("REG-200", "DOC-200"))
            .Returns(MayBe.From(model));

        var fakePdfBytes = "%PDF-1.4"u8.ToArray();
        _retrievalMock.Setup(x => x.RetrievePatientSignedPdfAsync(model.SigningRequestId, model.SignedDocUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakePdfBytes);

        _oftaClientMock.Setup(x => x.ArchiveDocAsync(
                model.OftaDocId, fakePdfBytes, model.FileName, model.RegId, model.DokumenId, model.ExternalDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ARCH-200");

        var worker = CreateWorker();
        var handler = new GeneralConsentArchiveTriggerHandler(worker);

        var cmd = new GeneralConsentArchiveTriggerCmd(
            RegId: "REG-200",
            DokumenId: "DOC-200",
            TriggerType: "ON_DEMAND",
            UserId: "user1");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.ProcessedCount.Should().Be(1);
        result.SucceededCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        result.Details.First().ArchiveId.Should().Be("ARCH-200");
    }
}

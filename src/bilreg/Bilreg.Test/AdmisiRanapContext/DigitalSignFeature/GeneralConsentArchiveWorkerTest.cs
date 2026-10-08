using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
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

public class GeneralConsentArchiveWorkerTest
{
    private readonly Mock<IRanapDigitalSignRepo> _repoMock = new();
    private readonly Mock<IOftaGeneralConsentClient> _oftaClientMock = new();
    private readonly Mock<IPatientSignedDocumentRetrievalService> _retrievalMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamMock = new();
    private readonly Mock<ILogger<GeneralConsentArchiveWorker>> _loggerMock = new();

    private readonly DateTime _now = new(2026, 10, 8, 15, 30, 0);

    public GeneralConsentArchiveWorkerTest()
    {
        _tglJamMock.Setup(x => x.Now).Returns(_now);
    }

    private GeneralConsentArchiveWorker CreateSut() => new(
        _repoMock.Object,
        _oftaClientMock.Object,
        _retrievalMock.Object,
        _auditRepoMock.Object,
        _tglJamMock.Object,
        _loggerMock.Object);

    private static RanapDigitalSignModel CreateModel(
        string signingRequestId = "b14e668c-ff5d-4952-b80c-0d35ee414777",
        string regId = "REG-001",
        string dokumenId = "DOC-001",
        string oftaSignState = "Signed",
        string patientSignState = "Signed",
        bool isArchived = false,
        string archiveId = "")
    {
        var model = RanapDigitalSignModel.CatatOftaProxy(
            regId: regId,
            hisReference: regId,
            dokumenId: dokumenId,
            signingRequestId: signingRequestId,
            pasien: new PasienReff("PSN-001", "Budi", new DateOnly(1990, 1, 1), "L"),
            signerId: "SGN-001",
            fileName: "general-consent.pdf",
            oftaDocId: "OFTA-DOC-123",
            oftaDocState: "Uploaded",
            oftaSignState: oftaSignState,
            officerRef: "officer@hospital.com",
            officerEmail: "officer@hospital.com",
            officerName: "Officer Satu",
            externalDocumentId: "EXT-001",
            signedDocUrl: "http://ofta/signed/123.pdf",
            auditUserId: "user1",
            createdAt: new DateTime(2026, 10, 8, 10, 0, 0),
            patientSignState: patientSignState);

        if (isArchived)
        {
            model = model.SetArchiveStatus(archiveId, "system", new DateTime(2026, 10, 8, 12, 0, 0));
        }

        return model;
    }

    [Fact]
    public async Task ProcessOneAsync_TerminalEligible_RetrievesPdf_ArchivesOfta_AndSavesStatus()
    {
        // Arrange
        var model = CreateModel(oftaSignState: "Signed", patientSignState: "Signed");
        _repoMock.Setup(x => x.LoadEntity(It.Is<IRanapDigitalSignKey>(k => k.SigningRequestId == model.SigningRequestId)))
            .Returns(MayBe.From(model));

        var fakePdfBytes = "%PDF-1.4-completed"u8.ToArray();
        _retrievalMock.Setup(x => x.RetrievePatientSignedPdfAsync(model.SigningRequestId, model.SignedDocUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakePdfBytes);

        _oftaClientMock.Setup(x => x.ArchiveDocAsync(
                model.OftaDocId, fakePdfBytes, model.FileName, model.RegId, model.DokumenId, model.ExternalDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ARCH-OFTA-123");

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessOneAsync(model.SigningRequestId, "WORKER", "SYSTEM_WORKER");

        // Assert
        result.Success.Should().BeTrue();
        result.IsAlreadyArchived.Should().BeFalse();
        result.ArchiveId.Should().Be("ARCH-OFTA-123");

        _repoMock.Verify(x => x.SaveChanges(It.Is<RanapDigitalSignModel>(m =>
            m.IsArchived &&
            m.ArchiveId == "ARCH-OFTA-123" &&
            m.ArchiveDate == _now)), Times.Once);

        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "GENERAL_CONSENT_ARCHIVE" &&
            a.EntityId.Contains("ARCH-OFTA-123"))), Times.Once);
    }

    [Fact]
    public async Task ProcessOneAsync_AlreadyArchived_ReturnsExistingArchiveId_Idempotent()
    {
        // Arrange
        var model = CreateModel(isArchived: true, archiveId: "ARCH-EXISTING-999");
        _repoMock.Setup(x => x.LoadEntity(It.Is<IRanapDigitalSignKey>(k => k.SigningRequestId == model.SigningRequestId)))
            .Returns(MayBe.From(model));

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessOneAsync(model.SigningRequestId);

        // Assert
        result.Success.Should().BeTrue();
        result.IsAlreadyArchived.Should().BeTrue();
        result.ArchiveId.Should().Be("ARCH-EXISTING-999");

        _retrievalMock.Verify(x => x.RetrievePatientSignedPdfAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _oftaClientMock.Verify(x => x.ArchiveDocAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }

    [Theory]
    [InlineData("Signed", "Waiting")]
    [InlineData("Signed", "")]
    [InlineData("Unsigned", "Signed")]
    [InlineData("Created", "Waiting")]
    public async Task ProcessOneAsync_IncompleteStatus_SuppressesArchive(string oftaSignState, string patientSignState)
    {
        // Arrange
        var model = CreateModel(oftaSignState: oftaSignState, patientSignState: patientSignState);
        _repoMock.Setup(x => x.LoadEntity(It.Is<IRanapDigitalSignKey>(k => k.SigningRequestId == model.SigningRequestId)))
            .Returns(MayBe.From(model));

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessOneAsync(model.SigningRequestId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Archive suppressed");

        _retrievalMock.Verify(x => x.RetrievePatientSignedPdfAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _oftaClientMock.Verify(x => x.ArchiveDocAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }

    [Fact]
    public async Task ProcessOneAsync_RetrievalFails_RecordsFailure_DoesNotClaimArchived()
    {
        // Arrange
        var model = CreateModel(oftaSignState: "Signed", patientSignState: "Signed");
        _repoMock.Setup(x => x.LoadEntity(It.Is<IRanapDigitalSignKey>(k => k.SigningRequestId == model.SigningRequestId)))
            .Returns(MayBe.From(model));

        _retrievalMock.Setup(x => x.RetrievePatientSignedPdfAsync(model.SigningRequestId, model.SignedDocUrl, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Download timeout"));

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessOneAsync(model.SigningRequestId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("PDF retrieval failed: Download timeout");

        _oftaClientMock.Verify(x => x.ArchiveDocAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }

    [Fact]
    public async Task ProcessOneAsync_OftaArchiveFails_RecordsFailure_DoesNotClaimArchived()
    {
        // Arrange
        var model = CreateModel(oftaSignState: "Signed", patientSignState: "Signed");
        _repoMock.Setup(x => x.LoadEntity(It.Is<IRanapDigitalSignKey>(k => k.SigningRequestId == model.SigningRequestId)))
            .Returns(MayBe.From(model));

        var fakePdfBytes = "%PDF-1.4-completed"u8.ToArray();
        _retrievalMock.Setup(x => x.RetrievePatientSignedPdfAsync(model.SigningRequestId, model.SignedDocUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakePdfBytes);

        _oftaClientMock.Setup(x => x.ArchiveDocAsync(
                model.OftaDocId, fakePdfBytes, model.FileName, model.RegId, model.DokumenId, model.ExternalDocumentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("OFTA 500 server error"));

        var sut = CreateSut();

        // Act
        var result = await sut.ProcessOneAsync(model.SigningRequestId);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("OFTA archive failed: OFTA 500 server error");

        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatchAsync_ProcessesPendingItemsAndResumesNextTime()
    {
        // Arrange
        var item1 = CreateModel("b14e668c-ff5d-4952-b80c-0d35ee414701", "REG-01", "DOC-01", "Signed", "Signed");
        var item2 = CreateModel("b14e668c-ff5d-4952-b80c-0d35ee414702", "REG-02", "DOC-02", "Signed", "Signed");

        _repoMock.Setup(x => x.ListPendingArchive(20)).Returns([item1, item2]);

        var fakePdfBytes = "%PDF-1.4"u8.ToArray();
        _retrievalMock.Setup(x => x.RetrievePatientSignedPdfAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakePdfBytes);

        _oftaClientMock.Setup(x => x.ArchiveDocAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("ARCH-BATCH-OK");

        var sut = CreateSut();

        // Act
        var batchResult = await sut.ProcessBatchAsync(batchSize: 20);

        // Assert
        batchResult.ProcessedCount.Should().Be(2);
        batchResult.SucceededCount.Should().Be(2);
        batchResult.FailedCount.Should().Be(0);

        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Exactly(2));
    }
}

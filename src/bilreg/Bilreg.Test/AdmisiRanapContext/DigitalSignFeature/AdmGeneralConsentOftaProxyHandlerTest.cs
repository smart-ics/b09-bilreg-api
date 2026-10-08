using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class AdmGeneralConsentOftaProxyHandlerTest
{
    private readonly Mock<IRanapDigitalSignRepo> _repoMock = new();
    private readonly Mock<IAdmissionRepo> _admissionMock = new();
    private readonly Mock<IAuditRepo> _auditMock = new();
    private readonly Mock<IOftaGeneralConsentClient> _oftaClientMock = new();

    private AdmGeneralConsentOftaProxyHandler CreateHandler() =>
        new(_repoMock.Object, _admissionMock.Object, _oftaClientMock.Object, _auditMock.Object, TestTglJamProvider.Instance);

    private static AdmissionModel SampleAdmission(string regId = "RG00000001") =>
        AdmissionModel.Admit(
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new KelasDkType("1", "Kelas 1"),
            new BangsalReff("B1", "Bangsal A"),
            null, null, "user1") with { RegId = regId };

    private static AdmGeneralConsentOftaProxyCmd SampleCmd() =>
        new(
            RegId: "RG00000001",
            DokumenId: "GC-001",
            ExternalDocumentId: "EXT-DOC-001",
            OfficerRef: "OFF-01",
            SignPositionDesc: "Tanda Tangan Petugas",
            FileBytes: new byte[] { 1, 2, 3, 4 },
            FileName: "gc.pdf",
            SignTag: null,
            SignPosition: null,
            DocTypeId: null,
            DocName: null,
            Passphrase: "secret-passphrase",
            Otp: "123456",
            PatientSignerId: "SIGNER-123",
            SigningRequestId: null,
            HisReference: "RG00000001",
            UserId: "user1"
        );

    private void SetupAdmission(string regId = "RG00000001")
    {
        var admission = SampleAdmission(regId);
        _admissionMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(admission));
    }

    [Fact]
    public async Task GivenAdmissionNotFound_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        _admissionMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<AdmissionModel>.None);

        var handler = CreateHandler();
        var act = () => handler.Handle(SampleCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Admission 'RG00000001' tidak ditemukan*");
    }

    [Fact]
    public async Task GivenConflictingActiveCorrelationWithDifferentExternalId_WhenHandle_ThenThrowsInvalidOperationException()
    {
        SetupAdmission();
        var activeModel = RanapDigitalSignModel.CatatOftaProxy(
            "RG00000001", "RG00000001", "GC-001", Guid.NewGuid().ToString("D"),
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            "SIGNER-123", "gc.pdf", "OFTA-1", "INGESTED", "IN_PROGRESS",
            "OFF-01", "officer@mail.com", "Officer", "EXT-DOC-DIFFERENT", "", "user1");

        _repoMock
            .Setup(x => x.LoadByRegDokumen("RG00000001", "GC-001"))
            .Returns(MayBe.From(activeModel));

        var handler = CreateHandler();
        var act = () => handler.Handle(SampleCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active General Consent for RegId 'RG00000001' and DokumenId 'GC-001' already exists with a different external correlation*");
    }

    [Fact]
    public async Task GivenSuccessfulIngestAndExecute_WhenHandle_ThenPersistsCorrelationAndAudits()
    {
        SetupAdmission();
        _repoMock
            .Setup(x => x.LoadByRegDokumen("RG00000001", "GC-001"))
            .Returns(MayBe<RanapDigitalSignModel>.None);

        _oftaClientMock
            .Setup(x => x.IngestAsync(It.IsAny<OftaGeneralConsentIngestClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentIngestClientResponse(
                DocId: "DOC-OFTA-999",
                DocState: "INGESTED",
                RequestedDocUrl: "http://ofta/requested/999.pdf",
                RegId: "RG00000001",
                DokumenId: "GC-001",
                ExternalDocumentId: "EXT-DOC-001",
                IsExisting: false));

        _oftaClientMock
            .Setup(x => x.ExecuteAsync(It.IsAny<OftaGeneralConsentExecuteClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentExecuteClientResponse(
                DocId: "DOC-OFTA-999",
                DocState: "COMPLETED",
                SignState: "SIGNED",
                SignedDocUrl: "http://ofta/signed/999.pdf",
                OfficerEmail: "officer@mail.com",
                OfficerName: "Officer Name",
                SignedDate: DateTime.Now,
                IsAlreadySigned: false));

        _oftaClientMock
            .Setup(x => x.DownloadSignedDocAsync("http://ofta/signed/999.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 10, 20, 30 });

        RanapDigitalSignModel? saved = null;
        _repoMock
            .Setup(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()))
            .Callback<RanapDigitalSignModel>(m => saved = m);

        var handler = CreateHandler();
        var result = await handler.Handle(SampleCmd(), CancellationToken.None);

        result.Should().NotBeNull();
        result.DocId.Should().Be("DOC-OFTA-999");
        result.SignState.Should().Be("SIGNED");
        result.SignedPdfBase64.Should().Be(Convert.ToBase64String(new byte[] { 10, 20, 30 }));

        saved.Should().NotBeNull();
        saved!.OftaDocId.Should().Be("DOC-OFTA-999");
        saved.ExternalDocumentId.Should().Be("EXT-DOC-001");
        saved.OftaSignState.Should().Be("SIGNED");

        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Once);
        _auditMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task GivenExecuteFails_WhenHandle_ThenExceptionPropagatesWithoutSaving()
    {
        SetupAdmission();
        _repoMock
            .Setup(x => x.LoadByRegDokumen("RG00000001", "GC-001"))
            .Returns(MayBe<RanapDigitalSignModel>.None);

        _oftaClientMock
            .Setup(x => x.IngestAsync(It.IsAny<OftaGeneralConsentIngestClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentIngestClientResponse(
                "DOC-OFTA-999", "INGESTED", "http://ofta/requested/999.pdf", "RG00000001", "GC-001", "EXT-DOC-001", false));

        _oftaClientMock
            .Setup(x => x.ExecuteAsync(It.IsAny<OftaGeneralConsentExecuteClientRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("OFTA service unavailable", null, System.Net.HttpStatusCode.BadGateway));

        var handler = CreateHandler();
        var act = () => handler.Handle(SampleCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();

        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }
}

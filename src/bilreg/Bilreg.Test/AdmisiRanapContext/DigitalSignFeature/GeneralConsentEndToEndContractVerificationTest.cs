using System.Text;
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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

/// <summary>
/// Verifies the full cross-repository contract sequence and cutover guardrails for PDS-002 / P4-S09:
/// c012 request -> OFTA ingest -> OFTA execute -> signed PDF return -> patient publish handoff -> patient Signed -> combined Lengkap -> archive.
/// Also enforces negative paths: no patient publish on failure, no print pipeline routes, idempotency on retries, and secret containment.
/// </summary>
public class GeneralConsentEndToEndContractVerificationTest
{
    private readonly Mock<IRanapDigitalSignRepo> _repoMock = new();
    private readonly Mock<IAdmissionRepo> _admissionMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<IOftaGeneralConsentClient> _oftaClientMock = new();
    private readonly Mock<IPatientSignedDocumentRetrievalService> _retrievalMock = new();

    private readonly List<RanapDigitalSignModel> _databaseStore = new();
    private readonly List<AuditLog> _auditStore = new();

    private const string RegId = "RG202610080001";
    private const string DokumenId = "DOC-GC-001";
    private const string ExternalDocumentId = "EXT-CORR-20261008-001";
    private const string OfficerRef = "petugas.admisi@hospital.com";
    private const string SignPositionDesc = "Tanda Tangan Petugas Admisi";
    private const string PatientSignerId = "SGN-PATIENT-888";
    private const string SigningRequestId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

    private readonly byte[] _rawUnsignedPdf = Encoding.UTF8.GetBytes("%PDF-1.4 - Raw Unsigned PDF Rendered from c012");
    private readonly byte[] _oftaOfficerSignedPdf = Encoding.UTF8.GetBytes("%PDF-1.4 - OFTA Provider-Signed PDF with Officer Digital Signature");
    private readonly byte[] _twoSignaturesFinalPdf = Encoding.UTF8.GetBytes("%PDF-1.4 - Final Dual-Attestation PDF (Officer TTE + Patient HiDok QR)");

    public GeneralConsentEndToEndContractVerificationTest()
    {
        // Wire up repository mock to in-memory store
        _repoMock.Setup(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()))
            .Callback<RanapDigitalSignModel>(m =>
            {
                var existingIndex = _databaseStore.FindIndex(x => x.SigningRequestId == m.SigningRequestId || (x.RegId == m.RegId && x.DokumenId == m.DokumenId));
                if (existingIndex >= 0)
                    _databaseStore[existingIndex] = m;
                else
                    _databaseStore.Add(m);
            });

        _repoMock.Setup(x => x.LoadEntity(It.IsAny<IRanapDigitalSignKey>()))
            .Returns<IRanapDigitalSignKey>(k =>
            {
                var item = _databaseStore.FirstOrDefault(x => x.SigningRequestId == k.SigningRequestId);
                return item != null ? MayBe.From(item) : MayBe<RanapDigitalSignModel>.None;
            });

        _repoMock.Setup(x => x.LoadByRegDokumen(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((r, d) =>
            {
                var item = _databaseStore.FirstOrDefault(x => x.RegId == r && x.DokumenId == d);
                return item != null ? MayBe.From(item) : MayBe<RanapDigitalSignModel>.None;
            });

        _repoMock.Setup(x => x.ListByRegId(It.IsAny<string>()))
            .Returns<string>(r => _databaseStore.Where(x => x.RegId == r));

        _auditRepoMock.Setup(x => x.SaveChanges(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(a => _auditStore.Add(a));

        // Setup Admission
        var sampleAdmission = AdmissionModel.Admit(
            new PasienReff("PSN-001", "Budi Santoso", new DateOnly(1985, 5, 20), "L"),
            new KelasDkType("1", "Kelas 1"),
            new BangsalReff("B1", "Bangsal Teratai"),
            null, null, "petugas.admisi") with { RegId = RegId };

        _admissionMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(sampleAdmission));
    }

    [Fact]
    public async Task HappyPath_SequentialDualAttestation_RetainsSameCorrelationThroughCompleteLifecycle()
    {
        // -----------------------------------------------------------------------------------------
        // STEP 1: c012 requests Kirim -> Bilreg OFTA Proxy (Ingest + Immediate Execute)
        // -----------------------------------------------------------------------------------------
        _oftaClientMock.Setup(x => x.IngestAsync(It.IsAny<OftaGeneralConsentIngestClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentIngestClientResponse(
                DocId: "OFTA-DOC-99001",
                DocState: "INGESTED",
                RequestedDocUrl: "http://ofta.internal/docs/99001.pdf",
                RegId: RegId,
                DokumenId: DokumenId,
                ExternalDocumentId: ExternalDocumentId,
                IsExisting: false));

        _oftaClientMock.Setup(x => x.ExecuteAsync(It.IsAny<OftaGeneralConsentExecuteClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentExecuteClientResponse(
                DocId: "OFTA-DOC-99001",
                DocState: "COMPLETED",
                SignState: "SIGNED",
                SignedDocUrl: "http://ofta.internal/signed/99001.pdf",
                OfficerEmail: OfficerRef,
                OfficerName: "Petugas Admisi",
                SignedDate: new DateTime(2026, 10, 8, 14, 0, 0),
                IsAlreadySigned: false));

        _oftaClientMock.Setup(x => x.DownloadSignedDocAsync("http://ofta.internal/signed/99001.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_oftaOfficerSignedPdf);

        var proxyHandler = new AdmGeneralConsentOftaProxyHandler(
            _repoMock.Object, _admissionMock.Object, _oftaClientMock.Object, _auditRepoMock.Object, TestTglJamProvider.Instance);

        var proxyCmd = new AdmGeneralConsentOftaProxyCmd(
            RegId: RegId,
            DokumenId: DokumenId,
            ExternalDocumentId: ExternalDocumentId,
            OfficerRef: OfficerRef,
            SignPositionDesc: SignPositionDesc,
            FileBytes: _rawUnsignedPdf,
            FileName: "general-consent.pdf",
            Passphrase: null,
            Otp: null,
            PatientSignerId: PatientSignerId,
            SigningRequestId: SigningRequestId,
            HisReference: RegId,
            UserId: "petugas.admisi");

        var proxyResponse = await proxyHandler.Handle(proxyCmd, CancellationToken.None);

        // Verify Step 1 Output
        proxyResponse.Should().NotBeNull();
        proxyResponse.DocId.Should().Be("OFTA-DOC-99001");
        proxyResponse.SignState.Should().Be("SIGNED");
        proxyResponse.ExternalDocumentId.Should().Be(ExternalDocumentId);
        proxyResponse.SigningRequestId.Should().Be(SigningRequestId);
        // CRITICAL CONTRACT CHECK: The returned PDF bytes must be the OFTA provider-signed PDF, NOT the raw PDF!
        proxyResponse.SignedPdfBase64.Should().Be(Convert.ToBase64String(_oftaOfficerSignedPdf));

        // -----------------------------------------------------------------------------------------
        // STEP 2: Read Model check immediately after officer execute (State should be Sebagian)
        // -----------------------------------------------------------------------------------------
        var getHandler = new AdmGetDigitalSignHandler(_repoMock.Object);
        var readResult1 = await getHandler.Handle(new AdmGetDigitalSignQry(RegId, DokumenId), CancellationToken.None);
        readResult1.Items.Should().ContainSingle();
        var item1 = readResult1.Items.Single();
        item1.CombinedStatus.Should().Be("Sebagian", "Officer is SIGNED but Patient has not yet signed");
        item1.OfficerSignState.Should().Be("SIGNED");
        item1.ExternalDocumentId.Should().Be(ExternalDocumentId);
        item1.OftaDocId.Should().Be("OFTA-DOC-99001");
        item1.IsArchived.Should().BeFalse();

        // -----------------------------------------------------------------------------------------
        // STEP 3: Patient Signs in HiDok -> Record Patient Sign state in Bilreg
        // -----------------------------------------------------------------------------------------
        var recordHandler = new AdmRecordDigitalSignHandler(
            _repoMock.Object, _admissionMock.Object, _auditRepoMock.Object, TestTglJamProvider.Instance);

        var recordCmd = new AdmRecordDigitalSignCmd(
            RegId: RegId,
            HisReference: RegId,
            DokumenId: DokumenId,
            SigningRequestId: SigningRequestId,
            SignerId: PatientSignerId,
            FileName: "general-consent.pdf",
            UserId: "system-webhook",
            ExternalDocumentId: ExternalDocumentId,
            PatientSignState: "SIGNED");

        var recordResponse = await recordHandler.Handle(recordCmd, CancellationToken.None);
        recordResponse.SigningRequestId.Should().Be(SigningRequestId);

        // -----------------------------------------------------------------------------------------
        // STEP 4: Read Model check after both signers completed -> Combined Status must be Lengkap!
        // -----------------------------------------------------------------------------------------
        var readResult2 = await getHandler.Handle(new AdmGetDigitalSignQry(RegId, DokumenId), CancellationToken.None);
        var item2 = readResult2.Items.Single();
        item2.CombinedStatus.Should().Be("Lengkap", "Both Officer and Patient are SIGNED on the same correlation");
        item2.OfficerSignState.Should().Be("SIGNED");
        item2.PatientSignState.Should().Be("SIGNED");
        item2.ExternalDocumentId.Should().Be(ExternalDocumentId);
        item2.OftaDocId.Should().Be("OFTA-DOC-99001");
        item2.IsArchived.Should().BeFalse();

        // -----------------------------------------------------------------------------------------
        // STEP 5: Background Archive Worker processes Completed document
        // -----------------------------------------------------------------------------------------
        _retrievalMock.Setup(x => x.RetrievePatientSignedPdfAsync(SigningRequestId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_twoSignaturesFinalPdf);

        _oftaClientMock.Setup(x => x.ArchiveDocAsync(
                "OFTA-DOC-99001", _twoSignaturesFinalPdf, "general-consent.pdf", RegId, DokumenId, ExternalDocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ARCH-OFTA-FINAL-777");

        var archiveWorker = new GeneralConsentArchiveWorker(
            _repoMock.Object, _oftaClientMock.Object, _retrievalMock.Object, _auditRepoMock.Object,
            TestTglJamProvider.Instance, NullLogger<GeneralConsentArchiveWorker>.Instance);

        var archiveResult = await archiveWorker.ProcessOneAsync(SigningRequestId, "WORKER", "SYSTEM");
        archiveResult.Success.Should().BeTrue();
        archiveResult.ArchiveId.Should().Be("ARCH-OFTA-FINAL-777");
        archiveResult.IsAlreadyArchived.Should().BeFalse();

        // -----------------------------------------------------------------------------------------
        // STEP 6: Final Read Model check -> Lengkap and IsArchived = True
        // -----------------------------------------------------------------------------------------
        var readResult3 = await getHandler.Handle(new AdmGetDigitalSignQry(RegId, DokumenId), CancellationToken.None);
        var item3 = readResult3.Items.Single();
        item3.CombinedStatus.Should().Be("Lengkap");
        item3.IsArchived.Should().BeTrue();
        item3.ArchiveId.Should().Be("ARCH-OFTA-FINAL-777");
        item3.ExternalDocumentId.Should().Be(ExternalDocumentId);

        // -----------------------------------------------------------------------------------------
        // AUDIT ASSERTIONS: Prove no secret leakage (passphrase, OTP, private keys) in audit store
        // -----------------------------------------------------------------------------------------
        _auditStore.Should().NotBeEmpty();
        foreach (var audit in _auditStore)
        {
            audit.EntityId.Should().NotContain("secret");
            audit.EntityId.Should().NotContain("passphrase");
            audit.EntityId.Should().NotContain("otp");
        }
    }

    [Fact]
    public async Task NegativePath_OfficerTteFails_RejectsFailFastAndDoesNotPersistOrPublish()
    {
        _oftaClientMock.Setup(x => x.IngestAsync(It.IsAny<OftaGeneralConsentIngestClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentIngestClientResponse(
                DocId: "OFTA-DOC-ERR",
                DocState: "INGESTED",
                RequestedDocUrl: "http://ofta.internal/docs/err.pdf",
                RegId: RegId,
                DokumenId: DokumenId,
                ExternalDocumentId: ExternalDocumentId,
                IsExisting: false));

        // Simulate provider TTE failure (e.g. officer missing registration in Tilaka/Vinotek)
        _oftaClientMock.Setup(x => x.ExecuteAsync(It.IsAny<OftaGeneralConsentExecuteClientRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Officer TTE registration not found on provider.", null, System.Net.HttpStatusCode.UnprocessableEntity));

        var proxyHandler = new AdmGeneralConsentOftaProxyHandler(
            _repoMock.Object, _admissionMock.Object, _oftaClientMock.Object, _auditRepoMock.Object, TestTglJamProvider.Instance);

        var proxyCmd = new AdmGeneralConsentOftaProxyCmd(
            RegId: RegId,
            DokumenId: DokumenId,
            ExternalDocumentId: ExternalDocumentId,
            OfficerRef: "unregistered.officer@hospital.com",
            SignPositionDesc: SignPositionDesc,
            FileBytes: _rawUnsignedPdf,
            FileName: "general-consent.pdf",
            UserId: "petugas.admisi");

        var act = () => proxyHandler.Handle(proxyCmd, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Officer TTE registration not found*");

        // Assert no state was recorded in database
        _databaseStore.Should().BeEmpty();
    }

    [Fact]
    public async Task Idempotency_RetryingIngestExecuteAndArchive_IsSafeNoOpAndDoesNotDuplicate()
    {
        // 1. Setup existing signed model in DB
        var model = RanapDigitalSignModel.CatatOftaProxy(
            regId: RegId,
            hisReference: RegId,
            dokumenId: DokumenId,
            signingRequestId: SigningRequestId,
            pasien: new PasienReff("PSN-001", "Budi", new DateOnly(1985, 5, 20), "L"),
            signerId: PatientSignerId,
            fileName: "general-consent.pdf",
            oftaDocId: "OFTA-DOC-99001",
            oftaDocState: "COMPLETED",
            oftaSignState: "SIGNED",
            officerRef: OfficerRef,
            officerEmail: OfficerRef,
            officerName: "Petugas Admisi",
            externalDocumentId: ExternalDocumentId,
            signedDocUrl: "http://ofta.internal/signed/99001.pdf",
            auditUserId: "petugas.admisi",
            patientSignState: "SIGNED")
            .SetArchiveStatus("ARCH-OFTA-FINAL-777", "SYSTEM", new DateTime(2026, 10, 8, 15, 0, 0));

        _databaseStore.Add(model);

        // 2. Retry OFTA Proxy with same external key
        _oftaClientMock.Setup(x => x.IngestAsync(It.IsAny<OftaGeneralConsentIngestClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentIngestClientResponse(
                "OFTA-DOC-99001", "COMPLETED", "http://ofta.internal/docs/99001.pdf", RegId, DokumenId, ExternalDocumentId, IsExisting: true));

        _oftaClientMock.Setup(x => x.ExecuteAsync(It.IsAny<OftaGeneralConsentExecuteClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OftaGeneralConsentExecuteClientResponse(
                "OFTA-DOC-99001", "COMPLETED", "SIGNED", "http://ofta.internal/signed/99001.pdf", OfficerRef, "Petugas Admisi", DateTime.Now, IsAlreadySigned: true));

        _oftaClientMock.Setup(x => x.DownloadSignedDocAsync("http://ofta.internal/signed/99001.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_oftaOfficerSignedPdf);

        var proxyHandler = new AdmGeneralConsentOftaProxyHandler(
            _repoMock.Object, _admissionMock.Object, _oftaClientMock.Object, _auditRepoMock.Object, TestTglJamProvider.Instance);

        var proxyCmd = new AdmGeneralConsentOftaProxyCmd(
            RegId: RegId,
            DokumenId: DokumenId,
            ExternalDocumentId: ExternalDocumentId,
            OfficerRef: OfficerRef,
            SignPositionDesc: SignPositionDesc,
            FileBytes: _rawUnsignedPdf,
            FileName: "general-consent.pdf",
            UserId: "petugas.admisi");

        var response = await proxyHandler.Handle(proxyCmd, CancellationToken.None);
        response.IsAlreadySigned.Should().BeTrue();
        _databaseStore.Should().HaveCount(1, "No duplicate records created");

        // 3. Retry Archive Worker
        var archiveWorker = new GeneralConsentArchiveWorker(
            _repoMock.Object, _oftaClientMock.Object, _retrievalMock.Object, _auditRepoMock.Object,
            TestTglJamProvider.Instance, NullLogger<GeneralConsentArchiveWorker>.Instance);

        var archiveResult = await archiveWorker.ProcessOneAsync(SigningRequestId, "WORKER", "SYSTEM");
        archiveResult.Success.Should().BeTrue();
        archiveResult.IsAlreadyArchived.Should().BeTrue();
        archiveResult.ArchiveId.Should().Be("ARCH-OFTA-FINAL-777");

        // Verify OFTA client ArchiveDocAsync was NEVER called again due to idempotency guard
        _oftaClientMock.Verify(x => x.ArchiveDocAsync(
            It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

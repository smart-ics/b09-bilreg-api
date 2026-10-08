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

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class AdmRecordDigitalSignHandlerTest
{
    private readonly Mock<IRanapDigitalSignRepo> _repoMock = new();
    private readonly Mock<IAdmissionRepo> _admissionMock = new();
    private readonly Mock<IAuditRepo> _auditMock = new();

    private AdmRecordDigitalSignHandler CreateHandler() =>
        new(_repoMock.Object, _admissionMock.Object, _auditMock.Object, TestTglJamProvider.Instance);

    private static AdmissionModel SampleAdmission(string regId = "RG00000001") =>
        AdmissionModel.Admit(
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            new KelasDkType("1", "Kelas 1"),
            new BangsalReff("B1", "Bangsal A"),
            null, null, "user1") with { RegId = regId };

    private static AdmRecordDigitalSignCmd SampleCmd(string? signingId = null, string? hisReference = "RG00000001") =>
        new("RG00000001", hisReference ?? string.Empty, "HIS-DOC-2026-001",
            signingId ?? Guid.NewGuid().ToString("D"),
            Guid.NewGuid().ToString("D"),
            "informed-consent.pdf",
            "user1");

    private void SetupAdmission(string regId = "RG00000001")
    {
        var admission = SampleAdmission(regId);
        _admissionMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(admission));
    }

    private void SetupEmptyRepo()
    {
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRanapDigitalSignKey>()))
            .Returns(MayBe<RanapDigitalSignModel>.None);
        _repoMock
            .Setup(x => x.LoadByRegDokumen(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(MayBe<RanapDigitalSignModel>.None);
    }

    [Fact]
    public async Task GivenAdmissionExists_WhenRecord_ThenSavesAndAudits()
    {
        SetupAdmission();
        SetupEmptyRepo();
        RanapDigitalSignModel? saved = null;
        _repoMock.Setup(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()))
            .Callback<RanapDigitalSignModel>(m => saved = m);

        var cmd = SampleCmd();
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.SigningRequestId.Should().Be(cmd.SigningRequestId);
        saved.Should().NotBeNull();
        saved!.RegId.Should().Be("RG00000001");
        saved.HisReference.Should().Be("RG00000001");
        saved.DokumenId.Should().Be("HIS-DOC-2026-001");
        _auditMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    [Fact]
    public async Task GivenSamePayloadTwice_WhenRecord_ThenIdempotent()
    {
        SetupAdmission();
        var cmd = SampleCmd();
        var existing = RanapDigitalSignModel.CatatCreated(
            cmd.RegId, cmd.RegId, cmd.DokumenId, cmd.SigningRequestId,
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            cmd.SignerId, cmd.FileName,
            cmd.UserId, TestTglJamProvider.Instance.Now);
        _repoMock
            .Setup(x => x.LoadEntity(It.Is<IRanapDigitalSignKey>(k => k.SigningRequestId == cmd.SigningRequestId)))
            .Returns(MayBe.From(existing));

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.SigningRequestId.Should().Be(cmd.SigningRequestId);
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenMissingAdmission_WhenRecord_ThenThrowsNotFound()
    {
        _admissionMock
            .Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<AdmissionModel>.None);

        var act = () => CreateHandler().Handle(SampleCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenSameDocDifferentSigningId_WhenRecord_ThenThrowsConflict()
    {
        SetupAdmission();
        var cmd = SampleCmd();
        var existing = RanapDigitalSignModel.CatatCreated(
            cmd.RegId, cmd.RegId, cmd.DokumenId, Guid.NewGuid().ToString("D"),
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            cmd.SignerId, cmd.FileName,
            cmd.UserId, TestTglJamProvider.Instance.Now);
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<IRanapDigitalSignKey>()))
            .Returns(MayBe<RanapDigitalSignModel>.None);
        _repoMock
            .Setup(x => x.LoadByRegDokumen(cmd.RegId, cmd.DokumenId))
            .Returns(MayBe.From(existing));

        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah tercatat*");
    }

    [Fact]
    public async Task GivenDifferentHisReference_WhenRecord_ThenSavedAsIs()
    {
        SetupAdmission();
        SetupEmptyRepo();
        RanapDigitalSignModel? saved = null;
        _repoMock.Setup(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()))
            .Callback<RanapDigitalSignModel>(m => saved = m);

        var result = await CreateHandler().Handle(SampleCmd(hisReference: "HIS-ENC-999"), CancellationToken.None);

        result.SigningRequestId.Should().NotBeEmpty();
        saved.Should().NotBeNull();
        saved!.RegId.Should().Be("RG00000001");
        saved.HisReference.Should().Be("HIS-ENC-999");
    }

    [Fact]
    public async Task GivenBlankHisReference_WhenRecord_ThenThrows()
    {
        SetupAdmission();
        SetupEmptyRepo();

        var act = () => CreateHandler().Handle(SampleCmd(hisReference: " "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*hisReference*");
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenExistingOftaRecord_WhenPatientPublishHandoff_ThenUpdatesSignerAndPreservesOftaCorrelation()
    {
        SetupAdmission();
        var signingId = Guid.NewGuid().ToString("D");
        var oftaRecord = RanapDigitalSignModel.CatatOftaProxy(
            regId: "RG00000001",
            hisReference: "RG00000001",
            dokumenId: "GC-001",
            signingRequestId: signingId,
            pasien: new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            signerId: "",
            fileName: "general-consent.pdf",
            oftaDocId: "DOC-OFTA-001",
            oftaDocState: "COMPLETED",
            oftaSignState: "SIGNED",
            officerRef: "OFF-1",
            officerEmail: "officer@mail.com",
            officerName: "Officer Name",
            externalDocumentId: "EXT-DOC-001",
            signedDocUrl: "http://ofta/signed/1.pdf",
            auditUserId: "petugas1");

        _repoMock
            .Setup(x => x.LoadByRegDokumen("RG00000001", "GC-001"))
            .Returns(MayBe.From(oftaRecord));

        RanapDigitalSignModel? saved = null;
        _repoMock.Setup(x => x.SaveChanges(It.IsAny<RanapDigitalSignModel>()))
            .Callback<RanapDigitalSignModel>(m => saved = m);

        var cmd = new AdmRecordDigitalSignCmd(
            RegId: "RG00000001",
            HisReference: "RG00000001",
            DokumenId: "GC-001",
            SigningRequestId: signingId,
            SignerId: "SIGNER-PATIENT-123",
            FileName: "gc-signed-officer.pdf",
            UserId: "petugas1",
            ExternalDocumentId: "EXT-DOC-001",
            PatientSignState: "PENDING");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.SigningRequestId.Should().Be(signingId);
        saved.Should().NotBeNull();
        saved!.OftaDocId.Should().Be("DOC-OFTA-001");
        saved.OftaSignState.Should().Be("SIGNED");
        saved.OfficerEmail.Should().Be("officer@mail.com");
        saved.ExternalDocumentId.Should().Be("EXT-DOC-001");
        saved.SignerId.Should().Be("SIGNER-PATIENT-123");
        saved.FileName.Should().Be("gc-signed-officer.pdf");
        saved.PatientSignState.Should().Be("PENDING");
        saved.CombinedStatus.Should().Be("Sebagian");

        _auditMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a => a.ActionType == "UPDATE_PATIENT_SIGN")), Times.Once);
    }

    [Fact]
    public async Task GivenConflictingExternalDocumentId_WhenRecord_ThenThrowsConflict()
    {
        SetupAdmission();
        var signingId = Guid.NewGuid().ToString("D");
        var oftaRecord = RanapDigitalSignModel.CatatOftaProxy(
            regId: "RG00000001",
            hisReference: "RG00000001",
            dokumenId: "GC-001",
            signingRequestId: signingId,
            pasien: new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            signerId: "",
            fileName: "general-consent.pdf",
            oftaDocId: "DOC-OFTA-001",
            oftaDocState: "COMPLETED",
            oftaSignState: "SIGNED",
            officerRef: "OFF-1",
            officerEmail: "officer@mail.com",
            officerName: "Officer Name",
            externalDocumentId: "EXT-DOC-ORIGINAL",
            signedDocUrl: "http://ofta/signed/1.pdf",
            auditUserId: "petugas1");

        _repoMock
            .Setup(x => x.LoadByRegDokumen("RG00000001", "GC-001"))
            .Returns(MayBe.From(oftaRecord));

        var cmd = new AdmRecordDigitalSignCmd(
            RegId: "RG00000001",
            HisReference: "RG00000001",
            DokumenId: "GC-001",
            SigningRequestId: signingId,
            SignerId: "SIGNER-PATIENT-123",
            FileName: "gc.pdf",
            UserId: "petugas1",
            ExternalDocumentId: "EXT-DOC-DIFFERENT");

        var act = () => CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*different external correlation*");
    }
}

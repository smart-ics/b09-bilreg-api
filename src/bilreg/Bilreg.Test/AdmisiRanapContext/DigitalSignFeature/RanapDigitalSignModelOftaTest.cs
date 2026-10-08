using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class RanapDigitalSignModelOftaTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    [Fact]
    public void GivenValidOftaProxy_WhenCatatOftaProxy_ThenModelCreatedWithStatusDraft()
    {
        var now = new DateTime(2026, 10, 8, 10, 0, 0);
        var signId = Guid.NewGuid().ToString("D");
        var model = RanapDigitalSignModel.CatatOftaProxy(
            regId: "RG202610080001",
            hisReference: "RG202610080001",
            dokumenId: "GC-20261008-001",
            signingRequestId: signId,
            pasien: SamplePasien(),
            signerId: "SIGNER-001",
            fileName: "general-consent.pdf",
            oftaDocId: "OFTA-DOC-12345",
            oftaDocState: "INGESTED",
            oftaSignState: "IN_PROGRESS",
            officerRef: "OFF-001",
            officerEmail: "officer@hospital.id",
            officerName: "Petugas Admisi",
            externalDocumentId: "EXT-DOC-001",
            signedDocUrl: "http://ofta/docs/123",
            auditUserId: "petugas1",
            createdAt: now);

        model.RegId.Should().Be("RG202610080001");
        model.HisReference.Should().Be("RG202610080001");
        model.DokumenId.Should().Be("GC-20261008-001");
        model.SigningRequestId.Should().Be(signId);
        model.SignerId.Should().Be("SIGNER-001");
        model.OftaDocId.Should().Be("OFTA-DOC-12345");
        model.OftaDocState.Should().Be("INGESTED");
        model.OftaSignState.Should().Be("IN_PROGRESS");
        model.OfficerRef.Should().Be("OFF-001");
        model.OfficerEmail.Should().Be("officer@hospital.id");
        model.OfficerName.Should().Be("Petugas Admisi");
        model.ExternalDocumentId.Should().Be("EXT-DOC-001");
        model.SignedDocUrl.Should().Be("http://ofta/docs/123");
        model.FileName.Should().Be("general-consent.pdf");
        model.AuditTrail.Created.UserId.Should().Be("petugas1");
        model.AuditTrail.Created.Timestamp.Should().Be(now);
    }

    [Fact]
    public void GivenBlankRegId_WhenCatatOftaProxy_ThenThrowsArgumentException()
    {
        var signId = Guid.NewGuid().ToString("D");
        var act = () => RanapDigitalSignModel.CatatOftaProxy(
            "", "RG202610080001", "GC-20261008-001", signId,
            SamplePasien(), "SIGNER-001", "general-consent.pdf",
            "OFTA-123", "STATE", "SIGN_STATE", "OFF-1", "email", "name", "ext", "url", "petugas1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenInvalidSigningRequestId_WhenCatatOftaProxy_ThenThrowsArgumentException()
    {
        var act = () => RanapDigitalSignModel.CatatOftaProxy(
            "RG202610080001", "RG202610080001", "GC-20261008-001", "invalid-guid",
            SamplePasien(), "SIGNER-001", "general-consent.pdf",
            "OFTA-123", "STATE", "SIGN_STATE", "OFF-1", "email", "name", "ext", "url", "petugas1");

        act.Should().Throw<ArgumentException>().WithMessage("*SigningRequestId*");
    }

    [Fact]
    public void GivenModel_WhenUpdateOftaExecution_ThenUpdatesExecutionFields()
    {
        var now = new DateTime(2026, 10, 8, 10, 0, 0);
        var signId = Guid.NewGuid().ToString("D");
        var model = RanapDigitalSignModel.CatatOftaProxy(
            "RG202610080001", "RG202610080001", "GC-20261008-001", signId,
            SamplePasien(), "SIGNER-001", "general-consent.pdf",
            "OFTA-12345", "INGESTED", "IN_PROGRESS", "OFF-1", "", "", "EXT-001", "", "petugas1", now);

        var execTime = now.AddMinutes(1);
        var updated = model.UpdateOftaExecution(
            oftaDocId: "OFTA-12345",
            oftaDocState: "COMPLETED",
            oftaSignState: "SIGNED",
            officerEmail: "officer@hospital.id",
            officerName: "Petugas Admisi",
            signedDocUrl: "http://ofta/signed/12345.pdf",
            userId: "petugas1",
            timestamp: execTime);

        updated.OftaDocState.Should().Be("COMPLETED");
        updated.OftaSignState.Should().Be("SIGNED");
        updated.OfficerEmail.Should().Be("officer@hospital.id");
        updated.OfficerName.Should().Be("Petugas Admisi");
        updated.SignedDocUrl.Should().Be("http://ofta/signed/12345.pdf");
        updated.AuditTrail.Modified.UserId.Should().Be("petugas1");
        updated.AuditTrail.Modified.Timestamp.Should().Be(execTime);
    }

    [Theory]
    [InlineData("SIGNED", "SIGNED", "Lengkap")]
    [InlineData("Signed", "signed", "Lengkap")]
    [InlineData("SIGNED", "PENDING", "Sebagian")]
    [InlineData("SIGNED", "", "Sebagian")]
    [InlineData("IN_PROGRESS", "SIGNED", "Sebagian")]
    [InlineData("", "SIGNED", "Sebagian")]
    [InlineData("", "", "Sebagian")]
    [InlineData("SIGNED", "REJECTED", "Sebagian")]
    [InlineData("REJECTED", "SIGNED", "Sebagian")]
    public void GivenVariousSignStates_WhenComputeCombinedStatus_ThenReturnsExpected(
        string officerSignState, string patientSignState, string expected)
    {
        var result = RanapDigitalSignModel.ComputeCombinedStatus(officerSignState, patientSignState);
        result.Should().Be(expected);
    }

    [Fact]
    public void GivenOftaProxyModel_WhenUpdatePatientSigning_ThenUpdatesSignerAndPatientState()
    {
        var now = new DateTime(2026, 10, 8, 10, 0, 0);
        var signId = Guid.NewGuid().ToString("D");
        var model = RanapDigitalSignModel.CatatOftaProxy(
            "RG202610080001", "RG202610080001", "GC-20261008-001", signId,
            SamplePasien(), "", "general-consent.pdf",
            "OFTA-12345", "COMPLETED", "SIGNED", "OFF-1", "officer@mail.com", "Officer", "EXT-001", "http://ofta/signed/1.pdf", "petugas1", now);

        model.CombinedStatus.Should().Be("Sebagian");

        var updateTime = now.AddMinutes(2);
        var updated = model.UpdatePatientSigning("SIGNER-PATIENT-1", "signed-gc.pdf", "SIGNED", "petugas1", updateTime);

        updated.SignerId.Should().Be("SIGNER-PATIENT-1");
        updated.FileName.Should().Be("signed-gc.pdf");
        updated.PatientSignState.Should().Be("SIGNED");
        updated.OftaSignState.Should().Be("SIGNED");
        updated.CombinedStatus.Should().Be("Lengkap");
        updated.AuditTrail.Modified.UserId.Should().Be("petugas1");
        updated.AuditTrail.Modified.Timestamp.Should().Be(updateTime);
    }
}

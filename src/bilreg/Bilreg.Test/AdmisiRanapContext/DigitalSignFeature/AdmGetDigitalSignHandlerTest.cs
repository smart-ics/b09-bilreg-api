using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.DigitalSignFeature;

public class AdmGetDigitalSignHandlerTest
{
    private readonly Mock<IRanapDigitalSignRepo> _repoMock = new();

    private static RanapDigitalSignModel Sample(string dokumenId, string? signingId = null) =>
        RanapDigitalSignModel.CatatCreated(
            "RG00000001", "HIS-ENC-999", dokumenId,
            signingId ?? Guid.NewGuid().ToString("D"),
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            Guid.NewGuid().ToString("D"),
            "file.pdf",
            "user1", TestTglJamProvider.Instance.Now);

    [Fact]
    public async Task GivenDocExists_WhenGetByRegAndDoc_ThenReturnsSingle()
    {
        var model = Sample("HIS-DOC-001");
        _repoMock.Setup(x => x.LoadByRegDokumen("RG00000001", "HIS-DOC-001"))
            .Returns(MayBe.From(model));

        var result = await new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry("RG00000001", "HIS-DOC-001"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        var item = result.Items.Single();
        item.SigningRequestId.Should().Be(model.SigningRequestId);
        item.HisReference.Should().Be("HIS-ENC-999");
        item.CombinedStatus.Should().Be("Sebagian");
        item.OftaDocId.Should().BeEmpty();
        item.OfficerSignState.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenTwoDocs_WhenGetByRegOnly_ThenReturnsList()
    {
        _repoMock.Setup(x => x.ListByRegId("RG00000001"))
            .Returns([Sample("HIS-DOC-001"), Sample("HIS-DOC-002")]);

        var result = await new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry("RG00000001"), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenCompletedTwoSignaturesDoc_WhenGet_ThenReturnsLengkapStatusAndRefs()
    {
        var model = RanapDigitalSignModel.CatatOftaProxy(
            regId: "RG00000001",
            hisReference: "RG00000001",
            dokumenId: "GC-001",
            signingRequestId: Guid.NewGuid().ToString("D"),
            pasien: new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            signerId: "SIGNER-PATIENT-1",
            fileName: "gc-signed.pdf",
            oftaDocId: "DOC-OFTA-001",
            oftaDocState: "COMPLETED",
            oftaSignState: "SIGNED",
            officerRef: "OFF-1",
            officerEmail: "officer@mail.com",
            officerName: "Officer Name",
            externalDocumentId: "EXT-DOC-001",
            signedDocUrl: "http://ofta/signed/1.pdf",
            auditUserId: "user1",
            patientSignState: "SIGNED")
            .SetArchiveStatus("ARCH-999", "system", new DateTime(2026, 10, 8, 12, 0, 0));

        _repoMock.Setup(x => x.LoadByRegDokumen("RG00000001", "GC-001"))
            .Returns(MayBe.From(model));

        var result = await new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry("RG00000001", "GC-001"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        var item = result.Items.Single();
        item.OfficerSignState.Should().Be("SIGNED");
        item.PatientSignState.Should().Be("SIGNED");
        item.CombinedStatus.Should().Be("Lengkap");
        item.OftaDocId.Should().Be("DOC-OFTA-001");
        item.ExternalDocumentId.Should().Be("EXT-DOC-001");
        item.OfficerRef.Should().Be("OFF-1");
        item.OfficerEmail.Should().Be("officer@mail.com");
        item.IsArchived.Should().BeTrue();
        item.ArchiveId.Should().Be("ARCH-999");
    }

    [Fact]
    public async Task GivenLegacyRecordWithoutOftaPair_WhenGet_ThenDoesNotSynthesizeOftaState()
    {
        // Legacy pre-cutover record
        var legacyModel = RanapDigitalSignModel.CatatCreated(
            "RG00000001", "RG00000001", "LEGACY-DOC-001", Guid.NewGuid().ToString("D"),
            new PasienReff("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            "SIGNER-1", "doc.pdf", "user1", TestTglJamProvider.Instance.Now,
            patientSignState: "SIGNED");

        _repoMock.Setup(x => x.LoadByRegDokumen("RG00000001", "LEGACY-DOC-001"))
            .Returns(MayBe.From(legacyModel));

        var result = await new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry("RG00000001", "LEGACY-DOC-001"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        var item = result.Items.Single();
        item.OftaDocId.Should().BeEmpty();
        item.OfficerSignState.Should().BeEmpty();
        item.PatientSignState.Should().Be("SIGNED");
        item.CombinedStatus.Should().Be("Sebagian"); // Not Lengkap because officer is not signed
    }

    [Fact]
    public async Task GivenNoKey_WhenGet_ThenThrows()
    {
        var act = () => new AdmGetDigitalSignHandler(_repoMock.Object)
            .Handle(new AdmGetDigitalSignQry(" "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

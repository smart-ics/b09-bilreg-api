using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultDocumentModelTest
{
    private static AuditInfoType Audit() => new("U1", new DateTime(2026, 5, 18, 10, 0, 0));

    [Fact]
    public void CreateInitial_SetsDraftAndVersionOne()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());

        doc.ResultDocumentId.Should().StartWith("LRD");
        doc.OrderId.Should().Be("LBO000000001");
        doc.VersionNo.Should().Be(1);
        doc.IsCurrentVersion.Should().BeTrue();
        doc.ResultStatus.Should().Be(LabResultStatusEnum.Draft);
        doc.ExpertiseNote.Should().BeEmpty();
        doc.CriticalNote.Should().BeEmpty();
        doc.Items.Should().BeEmpty();
    }

    [Fact]
    public void RecordResult_AssignsFlagsAndItems()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());

        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");

        doc.ResultSource.Should().Be(LabResultSourceEnum.Manual);
        doc.Items.Should().HaveCount(1);
        doc.Items[0].FlagStatus.Should().Be(LabResultFlagEnum.Normal);
        doc.Items[0].ItemNo.Should().Be(1);
        doc.Items[0].ComponentId.Should().Be("MLC0001");
    }

    [Fact]
    public void MarkRecorded_SetsStatusAndRecordedMeta()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Instrument, [LabResultTestSupport.Capture()], "U2");

        doc.MarkRecorded("U3");

        doc.ResultStatus.Should().Be(LabResultStatusEnum.Recorded);
        doc.RecordedUserId.Should().Be("U3");
        doc.RecordedDate.Should().BeAfter(new DateTime(2000, 1, 1));
    }

    [Fact]
    public void Verify_FromRecorded_SetsVerifiedAndMeta()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        var verifiedAt = new DateTime(2026, 5, 18, 14, 30, 0);

        doc.Verify("PATH1", verifiedAt);

        doc.ResultStatus.Should().Be(LabResultStatusEnum.Verified);
        doc.VerifiedUserId.Should().Be("PATH1");
        doc.VerifiedDate.Should().Be(verifiedAt);
    }

    [Fact]
    public void Verify_WithExpertiseNote_SetsExpertiseNote()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        var verifiedAt = new DateTime(2026, 5, 18, 14, 30, 0);

        doc.Verify("PATH1", verifiedAt, "Kesan: Anemia ringan");

        doc.ResultStatus.Should().Be(LabResultStatusEnum.Verified);
        doc.VerifiedUserId.Should().Be("PATH1");
        doc.VerifiedDate.Should().Be(verifiedAt);
        doc.ExpertiseNote.Should().Be("Kesan: Anemia ringan");
    }

    [Fact]
    public void Verify_WhenMandatoryComponentEmpty_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        var emptyMandatoryItem = new LabResultItemCapture(
            "MLC0001",
            "LTD0001",
            "Hemoglobin",
            "HB",
            "Hemoglobin",
            1,
            LabResultTypeEnum.Numeric,
            0m,
            "",
            "",
            "",
            "g/dL",
            "12-16",
            IsMandatory: true);

        doc.RecordResult(LabResultSourceEnum.Manual, [emptyMandatoryItem], "U2");
        doc.MarkRecorded("U3");

        var act = () => doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 30, 0));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Komponen mandatori belum terisi*Hemoglobin*");
    }

    [Fact]
    public void Verify_WhenNonMandatoryComponentEmpty_Succeeds()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        var optionalItem = new LabResultItemCapture(
            "MLC0002",
            "LTD0001",
            "Hemoglobin",
            "RETIC",
            "Reticulocyte",
            2,
            LabResultTypeEnum.Numeric,
            0m,
            "",
            "",
            "",
            "%",
            "0.5-1.5",
            IsMandatory: false);

        doc.RecordResult(LabResultSourceEnum.Manual, [optionalItem], "U2");
        doc.MarkRecorded("U3");

        doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 30, 0));

        doc.ResultStatus.Should().Be(LabResultStatusEnum.Verified);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));

        var act = () => doc.Verify("PATH2", new DateTime(2026, 5, 18, 15, 0, 0));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Verify_FromDraft_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");

        var act = () => doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Recorded*");
    }

    [Fact]
    public void RecordResult_WhenVerified_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));

        var act = () => doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U4");

        act.Should().Throw<InvalidOperationException>().WithMessage("*diverifikasi*");
    }

    [Fact]
    public void MarkRecorded_WhenVerified_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));

        var act = () => doc.MarkRecorded("U4");

        act.Should().Throw<InvalidOperationException>().WithMessage("*diverifikasi*");
    }

    [Fact]
    public void RecordResult_EmptyItems_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());

        var act = () => doc.RecordResult(LabResultSourceEnum.Manual, [], "U1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkReleased_FromVerified_SetsReleasedStatus()
    {
        var doc = VerifiedDocument();
        var releasedAt = new DateTime(2026, 5, 18, 15, 0, 0);

        doc.MarkReleased("KASIR1", releasedAt);

        doc.ResultStatus.Should().Be(LabResultStatusEnum.Released);
    }

    [Fact]
    public void MarkReleased_WhenNotVerified_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");

        var act = () => doc.MarkReleased("KASIR1", DateTime.Now);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Verified*");
    }

    [Fact]
    public void MarkReleased_WhenAlreadyReleased_Throws()
    {
        var doc = VerifiedDocument();
        doc.MarkReleased("KASIR1", DateTime.Now);

        var act = () => doc.MarkReleased("KASIR1", DateTime.Now);

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dirilis*");
    }

    [Fact]
    public void SetCriticalNote_SetsNoteProperly()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());

        doc.SetCriticalNote("  Hasil kritis sudah dilaporkan ke dr. Sp.A pada 14:15  ");

        doc.CriticalNote.Should().Be("Hasil kritis sudah dilaporkan ke dr. Sp.A pada 14:15");
    }

    [Fact]
    public void SetCriticalNote_WhenReleased_Throws()
    {
        var doc = VerifiedDocument();
        doc.MarkReleased("KASIR1", DateTime.Now);

        var act = () => doc.SetCriticalNote("Catatan baru");

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dirilis*");
    }

    [Fact]
    public void CreateFromLisIngestion_SetsVerifiedStatusAndLisValidator()
    {
        var validatedAt = new DateTime(2026, 5, 18, 12, 0, 0);
        var item = new LabResultItemModel(
            1,
            "MLC0001",
            "LTD0001",
            "Hemoglobin",
            "HB",
            "Hemoglobin",
            1,
            LabResultTypeEnum.Numeric,
            15.2m,
            "",
            "",
            "",
            "g/dL",
            "12-16",
            true,
            LabResultFlagEnum.Normal);

        var doc = LabResultDocumentModel.CreateFromLisIngestion(
            "LBO000000001",
            [item],
            "Dr. Validator LIS",
            validatedAt,
            Audit(),
            "Kritis: none",
            "Ekspertise: normal");

        doc.ResultDocumentId.Should().StartWith("LRD");
        doc.OrderId.Should().Be("LBO000000001");
        doc.VersionNo.Should().Be(1);
        doc.IsCurrentVersion.Should().BeTrue();
        doc.ResultSource.Should().Be(LabResultSourceEnum.ExternalLis);
        doc.ResultStatus.Should().Be(LabResultStatusEnum.Verified);
        doc.RecordedUserId.Should().Be("Dr. Validator LIS");
        doc.VerifiedUserId.Should().Be("Dr. Validator LIS");
        doc.RecordedDate.Should().Be(validatedAt);
        doc.VerifiedDate.Should().Be(validatedAt);
        doc.CriticalNote.Should().Be("Kritis: none");
        doc.ExpertiseNote.Should().Be("Ekspertise: normal");
        doc.Items.Should().HaveCount(1);
        doc.Items[0].NumericValue.Should().Be(15.2m);
    }

    private static LabResultDocumentModel VerifiedDocument()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));
        return doc;
    }

    [Fact]
    public void AmendVerifiedToNewVersion_CreatesNewVersionAndRetiresOld()
    {
        var doc = VerifiedDocument();
        doc.SetExpertiseNote("Ekspertise awal");
        doc.SetCriticalNote("Kritis awal");
        var amendedAt = new DateTime(2026, 5, 18, 16, 0, 0);
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var (retired, newVersion) = doc.AmendVerifiedToNewVersion(regenerated, "Koreksi nilai", "UAMEND", amendedAt);

        retired.ResultDocumentId.Should().Be(doc.ResultDocumentId);
        retired.IsCurrentVersion.Should().BeFalse();
        retired.ResultStatus.Should().Be(LabResultStatusEnum.Amended);
        retired.VerifiedUserId.Should().Be("PATH1");
        retired.ExpertiseNote.Should().Be("Ekspertise awal");
        retired.CriticalNote.Should().Be("Kritis awal");
        retired.Items.Should().HaveCount(1);
        retired.Items[0].NumericValue.Should().Be(14m);

        newVersion.ResultDocumentId.Should().NotBe(doc.ResultDocumentId);
        newVersion.VersionNo.Should().Be(2);
        newVersion.IsCurrentVersion.Should().BeTrue();
        newVersion.ResultStatus.Should().Be(LabResultStatusEnum.Recorded);
        newVersion.PreviousVersionId.Should().Be(doc.ResultDocumentId);
        newVersion.AmendmentReason.Should().Be("Koreksi nilai");
        newVersion.AmendedUserId.Should().Be("UAMEND");
        newVersion.VerifiedUserId.Should().BeEmpty();
        newVersion.ExpertiseNote.Should().Be("Ekspertise awal");
        newVersion.CriticalNote.Should().Be("Kritis awal");
        newVersion.Items.Should().HaveCount(1);
        newVersion.Items[0].ComponentId.Should().Be("MLC0001");
        newVersion.Items[0].NumericValue.Should().Be(0);
    }

    [Fact]
    public void AmendVerifiedToNewVersion_WhenReleased_Succeeds()
    {
        var doc = VerifiedDocument();
        doc.MarkReleased("KASIR1", new DateTime(2026, 5, 18, 15, 0, 0));
        var amendedAt = new DateTime(2026, 5, 18, 16, 0, 0);
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var (retired, newVersion) = doc.AmendVerifiedToNewVersion(regenerated, "Koreksi setelah rilis", "UAMEND", amendedAt);

        retired.ResultStatus.Should().Be(LabResultStatusEnum.Amended);
        retired.IsCurrentVersion.Should().BeFalse();
        newVersion.ResultStatus.Should().Be(LabResultStatusEnum.Recorded);
        newVersion.IsCurrentVersion.Should().BeTrue();
        newVersion.VersionNo.Should().Be(2);
    }

    [Fact]
    public void AmendVerifiedToNewVersion_WhenRecorded_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var act = () => doc.AmendVerifiedToNewVersion(regenerated, "Koreksi nilai pemeriksaan", "U1", DateTime.Now);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Verified*");
    }

    [Fact]
    public void AmendVerifiedToNewVersion_EmptyReason_Throws()
    {
        var doc = VerifiedDocument();
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var act = () => doc.AmendVerifiedToNewVersion(regenerated, "   ", "U1", DateTime.Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan amandemen wajib diisi minimal 10 karakter*");
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("pendek")]
    [InlineData("koreksi")]
    public void AmendVerifiedToNewVersion_ReasonLessThan10Characters_Throws(string shortReason)
    {
        var doc = VerifiedDocument();
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var act = () => doc.AmendVerifiedToNewVersion(regenerated, shortReason, "U1", DateTime.Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan amandemen wajib diisi minimal 10 karakter*");
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("Koreksi nilai analit hasil tes ulang")]
    public void AmendVerifiedToNewVersion_ReasonTenCharactersOrMore_Succeeds(string validReason)
    {
        var doc = VerifiedDocument();
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var (retired, newVersion) = doc.AmendVerifiedToNewVersion(regenerated, validReason, "U1", DateTime.Now);

        retired.ResultStatus.Should().Be(LabResultStatusEnum.Amended);
        newVersion.AmendmentReason.Should().Be(validReason.Trim());
        newVersion.ResultStatus.Should().Be(LabResultStatusEnum.Recorded);
    }
}

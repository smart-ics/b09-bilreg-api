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
        var amendedAt = new DateTime(2026, 5, 18, 16, 0, 0);
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var (retired, newVersion) = doc.AmendVerifiedToNewVersion(regenerated, "Koreksi nilai", "UAMEND", amendedAt);

        retired.ResultDocumentId.Should().Be(doc.ResultDocumentId);
        retired.IsCurrentVersion.Should().BeFalse();
        retired.ResultStatus.Should().Be(LabResultStatusEnum.Verified);
        retired.VerifiedUserId.Should().Be("PATH1");
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
        newVersion.Items.Should().HaveCount(1);
        newVersion.Items[0].ComponentId.Should().Be("MLC0001");
        newVersion.Items[0].NumericValue.Should().Be(0);
    }

    [Fact]
    public void AmendVerifiedToNewVersion_WhenRecorded_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.MarkRecorded("U3");
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var act = () => doc.AmendVerifiedToNewVersion(regenerated, "reason", "U1", DateTime.Now);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Verified*");
    }

    [Fact]
    public void AmendVerifiedToNewVersion_EmptyReason_Throws()
    {
        var doc = VerifiedDocument();
        var regenerated = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(LabOrderTestSupport.CreateEmrOrder()));

        var act = () => doc.AmendVerifiedToNewVersion(regenerated, "   ", "U1", DateTime.Now);

        act.Should().Throw<ArgumentException>();
    }
}

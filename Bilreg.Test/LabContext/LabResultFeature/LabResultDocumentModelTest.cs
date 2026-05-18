using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultDocumentModelTest
{
    private static AuditInfoType Audit() => new("U1", new DateTime(2026, 5, 18, 10, 0, 0));

    private static LabResultItemCapture Capture(
        string testId = "T1",
        string testName = "HB",
        LabResultTypeEnum type = LabResultTypeEnum.Numeric,
        decimal numeric = 14m,
        string refText = "12-16")
        => new(
            testId,
            testName,
            "HB",
            "Hemoglobin",
            type,
            numeric,
            "",
            "",
            "",
            "g/dL",
            refText);

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

        doc.RecordResult(LabResultSourceEnum.Manual, [Capture()], "U2");

        doc.ResultSource.Should().Be(LabResultSourceEnum.Manual);
        doc.Items.Should().HaveCount(1);
        doc.Items[0].FlagStatus.Should().Be(LabResultFlagEnum.Normal);
        doc.Items[0].ItemNo.Should().Be(1);
    }

    [Fact]
    public void MarkRecorded_SetsStatusAndRecordedMeta()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Instrument, [Capture()], "U2");

        doc.MarkRecorded("U3");

        doc.ResultStatus.Should().Be(LabResultStatusEnum.Recorded);
        doc.RecordedUserId.Should().Be("U3");
        doc.RecordedDate.Should().BeAfter(new DateTime(2000, 1, 1));
    }

    [Fact]
    public void Verify_FromRecorded_SetsVerifiedAndMeta()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [Capture()], "U2");
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
        doc.RecordResult(LabResultSourceEnum.Manual, [Capture()], "U2");
        doc.MarkRecorded("U3");
        doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));

        var act = () => doc.Verify("PATH2", new DateTime(2026, 5, 18, 15, 0, 0));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Verify_FromDraft_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [Capture()], "U2");

        var act = () => doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Recorded*");
    }

    [Fact]
    public void RecordResult_WhenVerified_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [Capture()], "U2");
        doc.MarkRecorded("U3");
        doc.Verify("PATH1", new DateTime(2026, 5, 18, 14, 0, 0));

        var act = () => doc.RecordResult(LabResultSourceEnum.Manual, [Capture()], "U4");

        act.Should().Throw<InvalidOperationException>().WithMessage("*diverifikasi*");
    }

    [Fact]
    public void MarkRecorded_WhenVerified_Throws()
    {
        var doc = LabResultDocumentModel.CreateInitial("LBO000000001", Audit());
        doc.RecordResult(LabResultSourceEnum.Manual, [Capture()], "U2");
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
}

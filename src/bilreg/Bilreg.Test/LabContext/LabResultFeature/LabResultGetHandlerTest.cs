using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultGetHandlerTest
{
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabResultDocumentRepo> _documentRepo = new();
    private readonly LabResultGetHandler _sut;

    public LabResultGetHandlerTest()
    {
        _sut = new LabResultGetHandler(_orderRepo.Object, _documentRepo.Object);
    }

    private static LabOrderModel SampleOrder(string orderId = "LBO000000001")
    {
        var snapshot = new PatientSnapshotType("REG1", "MR0001", "Pasien Uji", new DateTime(1990, 5, 20), "L", 36);
        return LabOrderTestSupport.CreateEmrOrder(
            orderId,
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenOrderIdNullOrWhitespace_ThrowsArgumentException(string? invalidOrderId)
    {
        var act = async () => await _sut.Handle(new LabResultGetQuery(invalidOrderId!), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe<LabOrderModel>.None);

        var act = async () => await _sut.Handle(new LabResultGetQuery("LBO999999999"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*LabOrder 'LBO999999999' not found*");
    }

    [Fact]
    public async Task Handle_WhenNoDocumentExists_ReturnsEmptyLabResultViewWithSafeDefaults()
    {
        var order = SampleOrder("LBO000000001");
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _documentRepo.Setup(x => x.LoadByOrderId(order.OrderId))
            .Returns(MayBe<LabResultDocumentModel>.None);

        var result = await _sut.Handle(new LabResultGetQuery(order.OrderId), CancellationToken.None);

        result.Should().NotBeNull();
        result.HasDocument.Should().BeFalse();
        result.OrderId.Should().Be(order.OrderId);
        result.ResultDocumentId.Should().BeEmpty();
        result.VersionNo.Should().Be(0);
        result.IsCurrentVersion.Should().BeFalse();
        result.ResultSource.Should().Be(0);
        result.ResultStatus.Should().Be(0);
        result.RecordedUserId.Should().BeEmpty();
        result.VerifiedUserId.Should().BeEmpty();
        result.ExpertiseNote.Should().BeEmpty();
        result.CriticalNote.Should().BeEmpty();
        result.AmendmentReason.Should().BeEmpty();
        result.PreviousVersionId.Should().BeEmpty();
        result.AmendedDate.Should().BeNull();
        result.AmendedUserId.Should().BeEmpty();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenDocumentExists_MapsAllFieldsIncludingClinicalNotesAndAmendmentAudit()
    {
        var order = SampleOrder("LBO000000001");
        var audit = new AuditInfoType("U_CREATOR", new DateTime(2026, 5, 18, 8, 0, 0));
        var doc = LabResultDocumentModel.CreateInitial(order.OrderId, audit);

        var capture = new LabResultItemCapture(
            ComponentId: "MLC0001",
            TestId: "LTD0001",
            TestName: "Hematologi Lengkap",
            ComponentCode: "HB",
            ComponentName: "Hemoglobin",
            SequenceNo: 1,
            ResultType: LabResultTypeEnum.Numeric,
            NumericValue: 13.5m,
            TextValue: "13.5",
            OptionValue: "",
            NarrativeValue: "",
            Unit: "g/dL",
            ReferenceRangeText: "12-16",
            IsMandatory: true);

        doc.RecordResult(LabResultSourceEnum.Manual, [capture], "U_ANALYST");
        doc.SetCriticalNote("Nilai kritis telah dilaporkan ke DPJP pada 10:15");
        doc.MarkRecorded("U_ANALYST");

        var verifiedAt = new DateTime(2026, 5, 18, 11, 0, 0);
        doc.Verify("DR_SPPK", verifiedAt, "Kesan: Anemia ringan normositik normokrom");

        var scaffoldItems = new LabResultScaffoldService().BuildStructureOnlyItems(
            new LabResultScaffoldService().BuildFromOrder(order));
        var amendedAt = new DateTime(2026, 5, 18, 14, 0, 0);
        var (retired, amendedDoc) = doc.AmendVerifiedToNewVersion(
            scaffoldItems,
            "Koreksi nilai analit hasil tes ulang duplo",
            "DR_SUPERVISOR",
            amendedAt);

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _documentRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(amendedDoc));

        var result = await _sut.Handle(new LabResultGetQuery(order.OrderId), CancellationToken.None);

        result.Should().NotBeNull();
        result.HasDocument.Should().BeTrue();
        result.ResultDocumentId.Should().Be(amendedDoc.ResultDocumentId);
        result.OrderId.Should().Be(order.OrderId);
        result.VersionNo.Should().Be(2);
        result.IsCurrentVersion.Should().BeTrue();
        result.ResultSource.Should().Be((int)LabResultSourceEnum.Manual);
        result.ResultStatus.Should().Be((int)LabResultStatusEnum.Recorded);
        result.RecordedUserId.Should().Be(amendedDoc.RecordedUserId);
        result.VerifiedUserId.Should().BeEmpty();
        result.ExpertiseNote.Should().Be("Kesan: Anemia ringan normositik normokrom");
        result.CriticalNote.Should().Be("Nilai kritis telah dilaporkan ke DPJP pada 10:15");
        result.AmendmentReason.Should().Be("Koreksi nilai analit hasil tes ulang duplo");
        result.PreviousVersionId.Should().Be(retired.ResultDocumentId);
        result.AmendedDate.Should().Be(amendedAt);
        result.AmendedUserId.Should().Be("DR_SUPERVISOR");

        result.Items.Should().HaveCount(1);
        var item = result.Items.First();
        item.ItemNo.Should().Be(1);
        item.ComponentId.Should().Be("MLC0001");
        item.TestId.Should().Be("LTD0001");
        item.TestName.Should().Be("Hemoglobin");
        item.ComponentCode.Should().Be("HB");
        item.ComponentName.Should().Be("Hemoglobin");
        item.Unit.Should().Be("g/dL");
        item.ReferenceRangeText.Should().Be("12-16");
        item.IsMandatory.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAmendedDateIsSentinel_MapsAmendedDateToNull()
    {
        var order = SampleOrder("LBO000000001");
        var audit = new AuditInfoType("U_CREATOR", new DateTime(2026, 5, 18, 8, 0, 0));
        var doc = LabResultDocumentModel.CreateInitial(order.OrderId, audit);
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U_ANALYST");
        doc.MarkRecorded("U_ANALYST");

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _documentRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(doc));

        var result = await _sut.Handle(new LabResultGetQuery(order.OrderId), CancellationToken.None);

        result.Should().NotBeNull();
        result.HasDocument.Should().BeTrue();
        result.AmendedDate.Should().BeNull();
        result.AmendedUserId.Should().BeEmpty();
        result.AmendmentReason.Should().BeEmpty();
        result.PreviousVersionId.Should().BeEmpty();
    }
}

using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultAmendHandlerTest
{
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabResultDocumentRepo> _resultRepo = new();
    private readonly LabResultAmendHandler _sut;

    public LabResultAmendHandlerTest()
    {
        _sut = new LabResultAmendHandler(
            _orderRepo.Object,
            _resultRepo.Object,
            new LabResultScaffoldService(), TestTglJamProvider.Instance);
    }

    private static LabOrderModel VerifiedOrder()
    {
        var snapshot = new PatientSnapshotType("R1", "P1", "Name", new DateTime(1990, 1, 1), "L", 30);
        var order = LabOrderTestSupport.CreateEmrOrder(
            "LAB001",
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U2");
        order.MarkCharged("TDK1", "U2");
        order.MarkRecorded("U3");
        order.MarkVerified("PATH");
        return order;
    }

    private static LabResultDocumentModel VerifiedResult(string orderId, string criticalNote = "Catatan Kritis 1", string expertiseNote = "Ekspertise Patologi 1")
    {
        var doc = LabResultDocumentModel.CreateInitial(orderId, new AuditInfoType("U1", DateTime.Now));
        doc.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        doc.SetCriticalNote(criticalNote);
        doc.MarkRecorded("U3");
        doc.Verify("PATH", new DateTime(2026, 5, 18, 14, 0, 0), expertiseNote);
        return doc;
    }

    [Fact]
    public async Task Handle_ValidRequest_SavesOrderRetiredAndNewVersion()
    {
        var order = VerifiedOrder();
        var result = VerifiedResult(order.OrderId, "Catatan Kritis 1", "Ekspertise Patologi 1");
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(result));

        await _sut.Handle(new LabResultAmendCmd(order.OrderId, "Koreksi nilai analit hasil tes ulang", "UAMEND"), CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Recorded)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(d =>
            d.IsCurrentVersion == false
            && d.ResultStatus == LabResultStatusEnum.Amended
            && d.CriticalNote == "Catatan Kritis 1"
            && d.ExpertiseNote == "Ekspertise Patologi 1")), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(d =>
            d.IsCurrentVersion
            && d.ResultStatus == LabResultStatusEnum.Recorded
            && d.VersionNo == 2
            && d.PreviousVersionId == result.ResultDocumentId
            && d.CriticalNote == "Catatan Kritis 1"
            && d.ExpertiseNote == "Ekspertise Patologi 1"
            && d.Items.Single().ComponentId == "MLC0001"
            && d.Items.Single().NumericValue == 0)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenResultReleased_AllowsAmendmentAndRetiresOldVersion()
    {
        var order = VerifiedOrder();
        order.Release("UREL", "Sudah bayar kasir");
        var result = VerifiedResult(order.OrderId, "Catatan Kritis Rilis", "Ekspertise Rilis");
        result.MarkReleased("UREL");

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(result));

        await _sut.Handle(new LabResultAmendCmd(order.OrderId, "Koreksi setelah rilis", "UAMEND"), CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Recorded)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(d =>
            d.IsCurrentVersion == false
            && d.ResultStatus == LabResultStatusEnum.Amended
            && d.CriticalNote == "Catatan Kritis Rilis"
            && d.ExpertiseNote == "Ekspertise Rilis")), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(d =>
            d.IsCurrentVersion
            && d.ResultStatus == LabResultStatusEnum.Recorded
            && d.VersionNo == 2
            && d.PreviousVersionId == result.ResultDocumentId
            && d.CriticalNote == "Catatan Kritis Rilis"
            && d.ExpertiseNote == "Ekspertise Rilis"
            && d.Items.Single().ComponentId == "MLC0001"
            && d.Items.Single().NumericValue == 0)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderCharged_Throws()
    {
        var order = LabOrderTestSupport.CreateEmrOrder(
            "LAB002",
            snapshot: new PatientSnapshotType("R1", "P1", "Name", new DateTime(1990, 1, 1), "L", 30),
            audit: new AuditInfoType("U1", DateTime.Now));
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _sut.Handle(new LabResultAmendCmd(order.OrderId, "Koreksi nilai analit hasil tes ulang", "U1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _resultRepo.Verify(x => x.SaveChanges(It.IsAny<LabResultDocumentModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResultNotVerified_Throws()
    {
        var order = VerifiedOrder();
        var result = LabResultDocumentModel.CreateInitial(order.OrderId, new AuditInfoType("U1", DateTime.Now));
        result.RecordResult(LabResultSourceEnum.Manual, [LabResultTestSupport.Capture()], "U2");
        result.MarkRecorded("U3");

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(result));

        var act = async () => await _sut.Handle(new LabResultAmendCmd(order.OrderId, "Koreksi nilai analit hasil tes ulang", "U1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Verified*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenReasonNullOrWhiteSpace_ThrowsArgumentException(string? invalidReason)
    {
        var act = async () => await _sut.Handle(new LabResultAmendCmd("LAB001", invalidReason!, "UAMEND"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _resultRepo.Verify(x => x.SaveChanges(It.IsAny<LabResultDocumentModel>()), Times.Never);
    }

    [Theory]
    [InlineData("pendek")]
    [InlineData("123456789")]
    public async Task Handle_WhenReasonLessThan10Characters_Throws(string invalidReason)
    {
        var order = VerifiedOrder();
        var result = VerifiedResult(order.OrderId);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(result));

        var act = async () => await _sut.Handle(new LabResultAmendCmd(order.OrderId, invalidReason, "UAMEND"), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan amandemen wajib diisi minimal 10 karakter*");
        _resultRepo.Verify(x => x.SaveChanges(It.IsAny<LabResultDocumentModel>()), Times.Never);
    }
}

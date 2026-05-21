using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultVerifyHandlerTest
{
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabResultDocumentRepo> _resultRepo = new();
    private readonly LabResultVerifyHandler _sut;

    public LabResultVerifyHandlerTest()
    {
        _sut = new LabResultVerifyHandler(_orderRepo.Object, _resultRepo.Object);
    }

    private static LabOrderModel RecordedLabOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        order.MarkRecorded("UR1");
        return order;
    }

    private static LabResultDocumentModel RecordedResultDocument(string orderId)
    {
        var doc = LabResultDocumentModel.CreateInitial(orderId, new AuditInfoType("U1", DateTime.Now));
        var cap = new LabResultItemCapture("T1", "HB", "", "", LabResultTypeEnum.Numeric, 13m, "", "", "", "g/dL", "12-16");
        doc.RecordResult(LabResultSourceEnum.Manual, [cap], "U1");
        doc.MarkRecorded("U2");
        return doc;
    }

    [Fact]
    public async Task Handle_Success_SavesOrderVerifiedAndDocumentVerified()
    {
        var order = RecordedLabOrder();
        var doc = RecordedResultDocument(order.OrderId);
        var verifiedAt = new DateTime(2026, 5, 18, 16, 0, 0);

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(doc));

        await _sut.Handle(new LabResultVerifyCmd(order.OrderId, "PATH1", verifiedAt), CancellationToken.None);

        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o => o.LabOrderStatus == LabOrderStatusEnum.Verified)), Times.Once);
        _resultRepo.Verify(x => x.SaveChanges(It.Is<LabResultDocumentModel>(r =>
            r.ResultStatus == LabResultStatusEnum.Verified
            && r.VerifiedUserId == "PATH1"
            && r.VerifiedDate == verifiedAt)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderNotRecorded_Throws()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000002", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _sut.Handle(
            new LabResultVerifyCmd(order.OrderId, "PATH1", new DateTime(2026, 5, 18, 12, 0, 0)),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Recorded*");
        _resultRepo.Verify(x => x.SaveChanges(It.IsAny<LabResultDocumentModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDocumentMissing_Throws()
    {
        var order = RecordedLabOrder();
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe<LabResultDocumentModel>.None);

        var act = async () => await _sut.Handle(
            new LabResultVerifyCmd(order.OrderId, "PATH1", new DateTime(2026, 5, 18, 12, 0, 0)),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        _orderRepo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyVerified_Throws()
    {
        var order = RecordedLabOrder();
        var doc = RecordedResultDocument(order.OrderId);
        doc.Verify("PATH0", new DateTime(2026, 5, 18, 10, 0, 0));

        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _resultRepo.Setup(x => x.LoadByOrderId(order.OrderId)).Returns(MayBe.From(doc));

        var act = async () => await _sut.Handle(
            new LabResultVerifyCmd(order.OrderId, "PATH1", new DateTime(2026, 5, 18, 12, 0, 0)),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*diverifikasi*");
    }
}

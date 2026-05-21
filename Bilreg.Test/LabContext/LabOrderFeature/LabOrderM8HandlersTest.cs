using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderM8HandlersTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly LabOrderApproveFinancialClearanceHandler _approveHandler;
    private readonly LabOrderRejectFinancialClearanceHandler _rejectHandler;
    private readonly LabOrderReleaseHandler _releaseHandler;

    public LabOrderM8HandlersTest()
    {
        _approveHandler = new LabOrderApproveFinancialClearanceHandler(_repo.Object);
        _rejectHandler = new LabOrderRejectFinancialClearanceHandler(_repo.Object);
        _releaseHandler = new LabOrderReleaseHandler(_repo.Object);
    }

    private static LabOrderModel VerifiedPendingOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        order.MarkRecorded("UR1");
        order.MarkVerified("PATH1");
        return order;
    }

    [Fact]
    public async Task ApproveHandler_Success_SavesApprovedClearance()
    {
        var order = VerifiedPendingOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        await _approveHandler.Handle(new LabOrderApproveFinancialClearanceCmd(order.OrderId, "FIN1"), CancellationToken.None);

        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.FinancialClearance == FinancialClearanceEnum.Approved
            && o.FinancialClearanceUserId == "FIN1")), Times.Once);
    }

    [Fact]
    public async Task RejectHandler_Success_SavesRejectedClearance()
    {
        var order = VerifiedPendingOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        await _rejectHandler.Handle(
            new LabOrderRejectFinancialClearanceCmd(order.OrderId, "FIN1", "Belum ok"),
            CancellationToken.None);

        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.FinancialClearance == FinancialClearanceEnum.Rejected
            && o.FinancialClearanceReason == "Belum ok")), Times.Once);
    }

    [Fact]
    public async Task ReleaseHandler_Success_SavesReleased()
    {
        var order = VerifiedPendingOrder();
        order.ApproveFinancialClearance("FIN1");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", "Catatan"),
            CancellationToken.None);

        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Released
            && o.ReleasedUserId == "REL1"
            && o.ReleaseNote == "Catatan")), Times.Once);
    }

    [Fact]
    public async Task ReleaseHandler_WhenPendingClearance_Throws()
    {
        var order = VerifiedPendingOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", ""),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Approved*");
        _repo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task ReleaseHandler_WhenAlreadyReleased_Throws()
    {
        var order = VerifiedPendingOrder();
        order.ApproveFinancialClearance("FIN1");
        order.Release("REL0", "");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", "x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Released*");
        _repo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }
}

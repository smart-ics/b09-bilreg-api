using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderM9HandlersTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly LabOrderCancelHandler _cancelHandler;
    private readonly LabOrderTerminateHandler _terminateHandler;

    public LabOrderM9HandlersTest()
    {
        _cancelHandler = new LabOrderCancelHandler(_repo.Object);
        _terminateHandler = new LabOrderTerminateHandler(_repo.Object);
    }

    private static LabOrderModel ChargedOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        return order;
    }

    private static LabOrderModel CollectedOrder()
    {
        var order = ChargedOrder();
        order.CollectSpecimen(
            "U9",
            new CollectionInfoType(new DateTime(2026, 5, 18, 12, 0, 0), "U9", ""));
        return order;
    }

    [Fact]
    public async Task CancelHandler_Success_SavesCancelled()
    {
        var order = ChargedOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        await _cancelHandler.Handle(
            new LabOrderCancelCmd(order.OrderId, "ADM1", "Salah order"),
            CancellationToken.None);

        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Cancelled
            && o.CancelledReason == "Salah order"
            && o.CancelledUserId == "ADM1")), Times.Once);
    }

    [Fact]
    public async Task CancelHandler_WhenCollected_ThrowsAndDoesNotSave()
    {
        var order = CollectedOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _cancelHandler.Handle(
            new LabOrderCancelCmd(order.OrderId, "ADM1", "x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task TerminateHandler_Success_SavesTerminated()
    {
        var order = CollectedOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        await _terminateHandler.Handle(
            new LabOrderTerminateCmd(order.OrderId, "LAB1", "Spesimen tidak cukup"),
            CancellationToken.None);

        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Terminated
            && o.TerminationReason == "Spesimen tidak cukup"
            && o.TerminationUserId == "LAB1")), Times.Once);
    }

    [Fact]
    public async Task TerminateHandler_WhenCharged_ThrowsAndDoesNotSave()
    {
        var order = ChargedOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _terminateHandler.Handle(
            new LabOrderTerminateCmd(order.OrderId, "LAB1", "x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task CancelHandler_WhenAlreadyCancelled_Throws()
    {
        var order = ChargedOrder();
        order.Cancel("U1", "Pertama");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _cancelHandler.Handle(
            new LabOrderCancelCmd(order.OrderId, "U2", "Kedua"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }
}

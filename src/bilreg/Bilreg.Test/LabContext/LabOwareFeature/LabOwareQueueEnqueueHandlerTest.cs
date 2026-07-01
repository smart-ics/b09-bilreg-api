using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOwareFeature;

public class LabOwareQueueEnqueueHandlerTest
{
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabOwareOutboundQueueRepo> _queueRepo = new();
    private readonly LabOwareQueueEnqueueHandler _handler;

    public LabOwareQueueEnqueueHandlerTest()
    {
        _handler = new LabOwareQueueEnqueueHandler(_orderRepo.Object, _queueRepo.Object);
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

    [Fact]
    public async Task Enqueue_CreatesPendingQueue_AndSetsOwarePending()
    {
        var order = ChargedOrder();
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var response = await _handler.Handle(
            new LabOwareQueueEnqueueCmd(order.OrderId, "U9"),
            CancellationToken.None);

        response.QueueId.Should().StartWith("LOQ");
        _queueRepo.Verify(x => x.SaveChanges(It.Is<LabOwareOutboundQueueModel>(q =>
            q.QueueStatus == LabOwareQueueStatusEnum.Pending
            && q.OrderId == order.OrderId
            && q.PayloadJson.Contains("LAB00000001"))), Times.Once);
        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.OwareStatus == OwareStatusEnum.Pending)), Times.Once);
    }

    [Fact]
    public async Task Enqueue_WhenOrdered_ThrowsAndDoesNotSave()
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000002", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _handler.Handle(
            new LabOwareQueueEnqueueCmd(order.OrderId, "U9"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _queueRepo.Verify(x => x.SaveChanges(It.IsAny<LabOwareOutboundQueueModel>()), Times.Never);
    }
}

using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Test.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOwareFeature;

public class LabOwareQueueProcessorTest
{
    private readonly Mock<ILabOwareOutboundQueueRepo> _queueRepo = new();
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabOwareIntegration> _integration = new();

    private LabOwareQueueProcessor CreateSut() =>
        new(_queueRepo.Object, _orderRepo.Object, _integration.Object);

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
    public void ProcessOne_Success_MarksQueueSucceededAndOrderSent()
    {
        var order = ChargedOrder();
        var queue = LabOwareOutboundQueueModel.CreatePending(order.OrderId, "{\"orderNo\":\"LAB1\"}");
        var queueId = queue.QueueId;

        SetupLoad(queue, order);
        _integration.Setup(x => x.Send(It.IsAny<string>())).Returns(new LabOwareSendResult(true, null));

        LabOwareOutboundQueueModel? savedQueue = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<LabOwareOutboundQueueModel>()))
            .Callback<LabOwareOutboundQueueModel>(q => savedQueue = q);

        var result = CreateSut().ProcessOne(queueId, LabOwareQueueProcessor.WorkerUserId);

        result.Success.Should().BeTrue();
        savedQueue!.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Succeeded);
        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.OwareStatus == OwareStatusEnum.Sent)), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessOne_Failure_SetsLastErrorAndOrderFailed()
    {
        var order = ChargedOrder();
        var queue = LabOwareOutboundQueueModel.CreatePending(order.OrderId, "OWARE_FAIL");
        var queueId = queue.QueueId;

        SetupLoad(queue, order);
        _integration.Setup(x => x.Send(It.IsAny<string>()))
            .Returns(new LabOwareSendResult(false, "send error"));

        LabOwareOutboundQueueModel? savedQueue = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<LabOwareOutboundQueueModel>()))
            .Callback<LabOwareOutboundQueueModel>(q => savedQueue = q);

        var result = CreateSut().ProcessOne(queueId, LabOwareQueueProcessor.WorkerUserId);

        result.Success.Should().BeFalse();
        savedQueue!.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Failed);
        savedQueue.LastError.Should().Be("send error");
        savedQueue.RetryCount.Should().Be(1);
    }

    [Fact]
    public void ProcessBatch_ProcessesListedItems()
    {
        var order = ChargedOrder();
        var q1 = LabOwareOutboundQueueModel.CreatePending(order.OrderId, "{\"a\":1}");
        var q2 = LabOwareOutboundQueueModel.CreatePending(order.OrderId, "{\"a\":2}");

        _queueRepo.Setup(x => x.ListProcessable(It.IsAny<int>()))
            .Returns([q1, q2]);

        var queues = new Dictionary<string, LabOwareOutboundQueueModel>
        {
            [q1.QueueId] = q1,
            [q2.QueueId] = q2
        };
        _queueRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOwareOutboundQueueKey>()))
            .Returns((ILabOwareOutboundQueueKey key) =>
                queues.TryGetValue(key.QueueId, out var q)
                    ? MayBe.From(q)
                    : MayBe<LabOwareOutboundQueueModel>.None);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));
        _integration.Setup(x => x.Send(It.IsAny<string>())).Returns(new LabOwareSendResult(true, null));

        var result = CreateSut().ProcessBatch(20, LabOwareQueueProcessor.WorkerUserId);

        result.ProcessedCount.Should().Be(2);
        result.SucceededCount.Should().Be(2);
    }

    private void SetupLoad(LabOwareOutboundQueueModel queue, LabOrderModel order)
    {
        _queueRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOwareOutboundQueueKey>()))
            .Returns((ILabOwareOutboundQueueKey key) =>
                key.QueueId == queue.QueueId
                    ? MayBe.From(queue)
                    : MayBe<LabOwareOutboundQueueModel>.None);
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>()))
            .Returns(MayBe.From(order));
    }
}

using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Application.LabContext.LabOwareFeature.Integration;
using Bilreg.Application.LabContext.LabOwareFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOwareFeature;

public class LabOwareRetryHandlerTest
{
    private readonly Mock<ILabOwareOutboundQueueRepo> _queueRepo = new();
    private readonly Mock<ILabOrderRepo> _orderRepo = new();
    private readonly Mock<ILabOwareIntegration> _integration = new();
    private readonly LabOwareRetryHandler _handler;

    public LabOwareRetryHandlerTest()
    {
        var processor = new LabOwareQueueProcessor(_queueRepo.Object, _orderRepo.Object, _integration.Object);
        _handler = new LabOwareRetryHandler(_queueRepo.Object, processor);
    }

    private static LabOrderModel ChargedOrder(string orderId = "LBO000000001")
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderItemModel.Create(
            "T1", "HB", "Hemoglobin", "TR1", "T-HB", "Tarif", VacutainerTypeEnum.Edta, "Blood", 1);
        var order = LabOrderModel.CreateFromEmr(snapshot, [item], "LAB00000001", new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        return order;
    }

    private static LabOwareOutboundQueueModel FailedQueue(string queueId, string orderId, string payload)
        => LabOwareOutboundQueueModel.Rehydrate(
            queueId,
            orderId,
            LabOwareOutboundQueueModel.DefaultMessageType,
            payload,
            LabOwareQueueStatusEnum.Failed,
            1,
            DateTime.Now,
            DateTime.Now,
            "prev err",
            DateTime.Now);

    [Fact]
    public async Task Retry_WhenFailed_Success_UpdatesOwareSent()
    {
        const string queueId = "LOQ000000001";
        var order = ChargedOrder();
        var queue = FailedQueue(queueId, order.OrderId, "{\"orderNo\":\"LAB1\"}");

        _queueRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOwareOutboundQueueKey>()))
            .Returns((ILabOwareOutboundQueueKey key) =>
            {
                if (key.QueueId == queueId)
                    return MayBe.From(queue);
                return MayBe<LabOwareOutboundQueueModel>.None;
            });
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _integration.Setup(x => x.Send(It.IsAny<string>())).Returns(new LabOwareSendResult(true, null));

        LabOwareOutboundQueueModel? savedQueue = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<LabOwareOutboundQueueModel>()))
            .Callback<LabOwareOutboundQueueModel>(q => savedQueue = q);

        var response = await _handler.Handle(
            new LabOwareRetryCmd(queueId, "U9"),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        savedQueue.Should().NotBeNull();
        savedQueue!.QueueStatus.Should().Be(LabOwareQueueStatusEnum.Succeeded);
        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.OwareStatus == OwareStatusEnum.Sent)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Retry_WhenFailed_SendFails_IncrementsRetryCount()
    {
        const string queueId = "LOQ000000002";
        var order = ChargedOrder();
        var queue = FailedQueue(queueId, order.OrderId, "OWARE_FAIL");

        _queueRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOwareOutboundQueueKey>()))
            .Returns(MayBe.From(queue));
        _orderRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _integration.Setup(x => x.Send(It.IsAny<string>()))
            .Returns(new LabOwareSendResult(false, "sim fail"));

        LabOwareOutboundQueueModel? savedQueue = null;
        _queueRepo.Setup(x => x.SaveChanges(It.IsAny<LabOwareOutboundQueueModel>()))
            .Callback<LabOwareOutboundQueueModel>(q => savedQueue = q);

        var response = await _handler.Handle(
            new LabOwareRetryCmd(queueId, "U9"),
            CancellationToken.None);

        response.Success.Should().BeFalse();
        savedQueue!.RetryCount.Should().Be(2);
        savedQueue.LastError.Should().Be("sim fail");
        _orderRepo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.OwareStatus == OwareStatusEnum.Failed)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Retry_WhenPending_Throws()
    {
        var queue = LabOwareOutboundQueueModel.CreatePending("LBO000000001", "{}");
        _queueRepo.Setup(x => x.LoadEntity(It.IsAny<ILabOwareOutboundQueueKey>()))
            .Returns(MayBe.From(queue));

        var act = async () => await _handler.Handle(
            new LabOwareRetryCmd(queue.QueueId, "U9"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

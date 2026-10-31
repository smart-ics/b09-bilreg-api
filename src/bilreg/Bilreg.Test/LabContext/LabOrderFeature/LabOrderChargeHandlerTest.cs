using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Domain.LabContext.LabOwareFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderChargeHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ILabBillingIntegration> _billingIntegration = new();
    private readonly Mock<ILabOwareOutboundQueueRepo> _queueRepo = new();
    private readonly LabOrderChargeHandler _sut;

    public LabOrderChargeHandlerTest()
    {
        _sut = new LabOrderChargeHandler(
            _repo.Object,
            _billingIntegration.Object,
            TestTglJamProvider.Instance,
            _queueRepo.Object);
    }

    private static LabOrderModel OrderedOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR0001", "Pasien", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderTestSupport.CreateEmrOrder(
            "LAB00000001",
            lines: [LabOrderTestSupport.ResolvedLine(item)],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));
    }

    private static LabOrderModel ExternalOrderedOrder()
    {
        var snapshot = new PatientSnapshotType(
            "LX26A14B8C", "EX-202610-00001", "Pasien Luar", new DateTime(1995, 5, 20), "P", 31);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderModel.CreateExternal(
            null,
            snapshot,
            [LabOrderTestSupport.ResolvedLine(item)],
            "LAB00000002",
            new AuditInfoType("U1", DateTime.Now));
    }

    private static LabOrderModel DeferredOrder()
    {
        var order = OrderedOrder();
        order.Defer("Puasa", new DateTime(2026, 5, 20), "U2");
        return order;
    }

    [Fact]
    public async Task Handle_ValidRequest_ChargedAndSaved()
    {
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.ChargeOrder(order, "U3", It.IsAny<DateTime>()))
            .Returns("TDK-REF-0001");
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.BillingTindakanId.Should().Be("TDK-REF-0001");
        result.BillingLastError.Should().BeNull();
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
        persisted.BillingTindakanId.Should().Be("TDK-REF-0001");
        _billingIntegration.Verify(
            i => i.ChargeOrder(order, "U3", It.IsAny<DateTime>()),
            Times.Once);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExternalPatient_DelegatesToBillingIntegration_AndSaves()
    {
        var order = ExternalOrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.ChargeOrder(order, "U3", It.IsAny<DateTime>()))
            .Returns("TDK-EXT-0001");
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.BillingTindakanId.Should().Be("TDK-EXT-0001");
        result.BillingLastError.Should().BeNull();
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
        persisted.BillingTindakanId.Should().Be("TDK-EXT-0001");
        persisted.Patient.RegId.Should().Be("LX26A14B8C");
        persisted.Patient.RegId.Length.Should().Be(10);
        _billingIntegration.Verify(
            i => i.ChargeOrder(order, "U3", It.IsAny<DateTime>()),
            Times.Once);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeferredOrder_ThrowsBeforeSave()
    {
        var order = DeferredOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _billingIntegration.Verify(
            i => i.ChargeOrder(It.IsAny<LabOrderModel>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyChargedOrder_ThrowsBeforeBilling()
    {
        var order = OrderedOrder();
        order.MarkCharged("TDK-PREV", "U1");
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _billingIntegration.Verify(
            i => i.ChargeOrder(It.IsAny<LabOrderModel>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BillingFailure_RecordsErrorWithoutAdvancingState()
    {
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.ChargeOrder(order, "U3", It.IsAny<DateTime>()))
            .Throws(new LabBillingChargeException("Tarif tidak aktif"));
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.BillingTindakanId.Should().BeNull();
        result.BillingLastError.Should().Be("Tarif tidak aktif");
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Ordered);
        persisted.BillingLastError.Should().Be("Tarif tidak aktif");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Throws()
    {
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe<LabOrderModel>.None);

        var act = async () => await _sut.Handle(
            new LabOrderChargeCmd("LBO000000999", "U3"),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        _billingIntegration.Verify(
            i => i.ChargeOrder(It.IsAny<LabOrderModel>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidInputs_Throws()
    {
        var act1 = async () => await _sut.Handle(
            new LabOrderChargeCmd("", "U1"),
            CancellationToken.None);
        var act2 = async () => await _sut.Handle(
            new LabOrderChargeCmd("LBO1", ""),
            CancellationToken.None);

        await act1.Should().ThrowAsync<ArgumentException>();
        await act2.Should().ThrowAsync<ArgumentException>();
        _billingIntegration.Verify(
            i => i.ChargeOrder(It.IsAny<LabOrderModel>(), It.IsAny<string>(), It.IsAny<DateTime>()),
            Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ChargeSuccess_CreatesOwareQueueAndSetsOwarePending()
    {
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration.Setup(i => i.ChargeOrder(order, "U1", It.IsAny<DateTime>())).Returns("TDK-999");

        var response = await _sut.Handle(new LabOrderChargeCmd(order.OrderId, "U1"), CancellationToken.None);

        response.Success.Should().BeTrue();
        order.OwareStatus.Should().Be(OwareStatusEnum.Pending);
        _queueRepo.Verify(q => q.SaveChanges(It.Is<LabOwareOutboundQueueModel>(m =>
            m.OrderId == order.OrderId &&
            m.QueueStatus == LabOwareQueueStatusEnum.Pending &&
            m.MessageType == LabOwareOutboundQueueModel.DefaultMessageType &&
            m.PayloadJson.Contains(order.OrderId))), Times.Once);
    }

    [Fact]
    public async Task Handle_ChargeFailure_DoesNotCreateOwareQueue()
    {
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration.Setup(i => i.ChargeOrder(order, "U1", It.IsAny<DateTime>()))
            .Throws(new LabBillingChargeException("Simulated error"));

        var response = await _sut.Handle(new LabOrderChargeCmd(order.OrderId, "U1"), CancellationToken.None);

        response.Success.Should().BeFalse();
        _queueRepo.Verify(q => q.SaveChanges(It.IsAny<LabOwareOutboundQueueModel>()), Times.Never);
    }
}

using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
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
    private readonly LabOrderChargeHandler _sut;

    public LabOrderChargeHandlerTest()
    {
        _sut = new LabOrderChargeHandler(_repo.Object, _billingIntegration.Object, TestTglJamProvider.Instance);
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
            .Setup(i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()))
            .Returns("TDK-FAKE-0001");
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        var result = await _sut.Handle(
            new LabOrderChargeCmd(order.OrderId, "U3"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.BillingTindakanId.Should().Be("TDK-FAKE-0001");
        result.BillingLastError.Should().BeNull();
        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Charged);
        persisted.BillingTindakanId.Should().Be("TDK-FAKE-0001");
        _billingIntegration.Verify(
            i => i.CreateTindakan(It.Is<LabBillingChargeRequest>(r =>
                r.OrderId == order.OrderId && r.UserId == "U3" && r.TarifLines.Count == 1)),
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
            i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()),
            Times.Never);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BillingFailure_RecordsErrorWithoutAdvancingState()
    {
        var order = OrderedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        _billingIntegration
            .Setup(i => i.CreateTindakan(It.IsAny<LabBillingChargeRequest>()))
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
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }
}

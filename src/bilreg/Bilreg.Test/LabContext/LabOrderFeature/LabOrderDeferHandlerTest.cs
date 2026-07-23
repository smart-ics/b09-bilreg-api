using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderDeferHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly LabOrderDeferHandler _sut;

    public LabOrderDeferHandlerTest()
    {
        _sut = new LabOrderDeferHandler(_repo.Object, TestTglJamProvider.Instance);
    }

    private static LabOrderModel OrderedOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR0001", "Pasien", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        return LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
    }

    [Fact]
    public async Task Handle_ValidRequest_DeferredAndSaved()
    {
        var order = OrderedOrder();
        var until = new DateTime(2026, 5, 20);
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        await _sut.Handle(new LabOrderDeferCmd(order.OrderId, "U2", "Puasa", until), CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Deferred);
        persisted.DeferredInfo.Reason.Should().Be("Puasa");
        persisted.DeferredInfo.Until.Should().Be(until);
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Throws()
    {
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe<LabOrderModel>.None);

        var act = async () => await _sut.Handle(
            new LabOrderDeferCmd("LBO000000999", "U2", "Puasa", DateTime.Now),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }
}

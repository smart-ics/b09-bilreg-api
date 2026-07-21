using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderCollectSpecimenHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly LabOrderCollectSpecimenHandler _sut;

    public LabOrderCollectSpecimenHandlerTest()
    {
        _sut = new LabOrderCollectSpecimenHandler(_repo.Object, TestTglJamProvider.Instance);
    }

    private static LabOrderModel ChargedOrder()
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR0001", "Pasien", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000001", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U2");
        order.MarkCharged("TDK-FAKE-0001", "U2");
        return order;
    }

    [Fact]
    public async Task Handle_ChargedOrder_PersistsCollected()
    {
        var order = ChargedOrder();
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));
        LabOrderModel? persisted = null;
        _repo.Setup(r => r.SaveChanges(It.IsAny<LabOrderModel>()))
            .Callback<LabOrderModel>(m => persisted = m);

        await _sut.Handle(
            new LabOrderCollectSpecimenCmd(order.OrderId, "U3", "OK", null),
            CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.LabOrderStatus.Should().Be(LabOrderStatusEnum.Collected);
        persisted.CollectionInfo.CollectedUserId.Should().Be("U3");
        persisted.CollectionInfo.CollectionNote.Should().Be("OK");
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Throws()
    {
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe<LabOrderModel>.None);

        var act = async () => await _sut.Handle(
            new LabOrderCollectSpecimenCmd("LBO000000999", "U3", null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotCharged_ThrowsBeforeSave()
    {
        var snapshot = new PatientSnapshotType(
            "REG001", "MR0001", "Pasien", new DateTime(1990, 1, 1), "L", 36);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder("LAB00000002", lines: [LabOrderTestSupport.ResolvedLine(item)], snapshot: snapshot, audit: new AuditInfoType("U1", DateTime.Now));
        _repo.Setup(r => r.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _sut.Handle(
            new LabOrderCollectSpecimenCmd(order.OrderId, "U3", null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }
}

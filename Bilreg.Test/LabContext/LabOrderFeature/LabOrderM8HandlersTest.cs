using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.Integration;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderM8HandlersTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly Mock<ILabBillingReleaseCheckDal> _checkDal = new();
    private readonly ILabBillingIntegration _billing = new LabBillingIntegration();
    private readonly LabOrderReleaseHandler _releaseHandler;

    public LabOrderM8HandlersTest()
    {
        _releaseHandler = new LabOrderReleaseHandler(_repo.Object, _billing, _checkDal.Object);
    }

    private static LabOrderModel VerifiedOrder(string orderNo = "LAB00000001")
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderItemModel.Create(
            "T1", "HB", "Hemoglobin", "TR1", "T-HB", "Tarif", VacutainerTypeEnum.Edta, "Blood", 1);
        var order = LabOrderModel.CreateFromEmr(snapshot, [item], orderNo, new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        order.MarkRecorded("UR1");
        order.MarkVerified("PATH1");
        return order;
    }

    [Fact]
    public async Task ReleaseHandler_WhenClear_ExecutesReleaseAndReturns200Payload()
    {
        var order = VerifiedOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var result = await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", "Catatan"),
            CancellationToken.None);

        result.Released.Should().BeTrue();
        result.BillingStatus.Should().Be(BillingReleaseStatusApi.Clear);
        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Released
            && o.ReleasedUserId == "REL1")), Times.Once);
        _checkDal.Verify(x => x.Insert(It.IsAny<BillingReleaseCheckModel>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseHandler_WhenBlocked_ReturnsOperationalPayloadWithoutRelease()
    {
        var order = VerifiedOrder("LAB-BLOCK-001");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var result = await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", ""),
            CancellationToken.None);

        result.Released.Should().BeFalse();
        result.BillingStatus.Should().Be(BillingReleaseStatusApi.Blocked);
        result.Message.Should().NotBeNullOrWhiteSpace();
        _repo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
        _checkDal.Verify(x => x.Insert(It.Is<BillingReleaseCheckModel>(c =>
            c.BillingStatus == BillingReleaseStatusEnum.Blocked)), Times.Once);
    }

    [Fact]
    public async Task ReleaseHandler_WhenNotVerified_Throws()
    {
        var order = VerifiedOrder();
        order.ReturnToRecordedAfterResultAmendment("U1");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", ""),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Verified*");
        _checkDal.Verify(x => x.Insert(It.IsAny<BillingReleaseCheckModel>()), Times.Never);
    }

    [Fact]
    public async Task ReleaseHandler_WhenAlreadyReleased_Throws()
    {
        var order = VerifiedOrder();
        order.Release("REL0", "");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var act = async () => await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", "x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Released*");
        _checkDal.Verify(x => x.Insert(It.IsAny<BillingReleaseCheckModel>()), Times.Never);
    }

    [Fact]
    public async Task ReleaseHandler_AfterAmendment_RevalidatesWithBil()
    {
        var order = VerifiedOrder("LAB-BLOCK-AMEND");
        order.Release("REL0", "");
        order.ReturnToRecordedAfterResultAmendment("UAMEND");
        order.MarkVerified("PATH2");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var result = await _releaseHandler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL2", ""),
            CancellationToken.None);

        result.Released.Should().BeFalse();
        result.BillingStatus.Should().Be(BillingReleaseStatusApi.Blocked);
        _checkDal.Verify(x => x.Insert(It.IsAny<BillingReleaseCheckModel>()), Times.Once);
    }
}

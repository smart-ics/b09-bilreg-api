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

public class LabOrderReleaseHandlerTest
{
    private readonly Mock<ILabOrderRepo> _repo = new();
    private readonly LabBillingIntegration _billing = new();

    private static LabOrderModel VerifiedOrder(string orderNo = "LAB00000001")
    {
        var snapshot = new PatientSnapshotType(
            "REG1", "MR1", "Pasien", new DateTime(1990, 1, 1), "L", 35);
        var item = LabOrderTestSupport.SampleItem();
        var order = LabOrderTestSupport.CreateEmrOrder(
            orderNo,
            lines: [LabOrderTestSupport.ResolvedLine(item)],
            snapshot: snapshot,
            audit: new AuditInfoType("U1", DateTime.Now));
        order.Charge("U1");
        order.MarkCharged("TDK1", "U1");
        order.MarkRecorded("UR1");
        order.MarkVerified("PATH1");
        return order;
    }

    [Fact]
    public async Task Release_WhenBilClear_ExecutesReleaseAndReturnsReleasedTrue()
    {
        var order = VerifiedOrder();
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var handler = new LabOrderReleaseHandler(_repo.Object, _billing);
        var response = await handler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", "Catatan"),
            CancellationToken.None);

        response.Released.Should().BeTrue();
        response.BillingStatus.Should().Be("CLEAR");
        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Released
            && o.LastBillingReleaseStatus == BillingReleaseValidationStatusEnum.Clear)), Times.Once);
    }

    [Fact]
    public async Task Release_WhenBilBlocked_ReturnsOperationalPayloadWithoutRelease()
    {
        var order = VerifiedOrder("LAB-BILBLOCK");
        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var handler = new LabOrderReleaseHandler(_repo.Object, _billing);
        var response = await handler.Handle(
            new LabOrderReleaseCmd(order.OrderId, "REL1", ""),
            CancellationToken.None);

        response.Released.Should().BeFalse();
        response.BillingStatus.Should().Be("BLOCKED");
        response.Message.Should().Contain("syarat release");
        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Verified
            && o.LastBillingReleaseStatus == BillingReleaseValidationStatusEnum.Blocked)), Times.Once);
    }

    [Fact]
    public async Task Release_WhenBilInfrastructureFails_PropagatesException()
    {
        var order = VerifiedOrder();
        order = LabOrderModel.Load(
            order.OrderId,
            order.EmrOrderId,
            order.OrderNo,
            order.OrderSource,
            order.LabOrderStatus,
            order.LastBillingReleaseStatus,
            order.OwareStatus,
            order.Patient,
            order.ExecutionRegId,
            order.DeferredInfo,
            order.BillingTindakanId,
            order.BillingLastError,
            order.CollectionInfo,
            order.LastBillingReleaseCheckAt,
            order.LastBillingReleaseCheckUserId,
            order.LastBillingReleaseMessage,
            order.ReleasedDate,
            order.ReleasedUserId,
            order.ReleaseNote,
            order.CancelledReason,
            order.CancelledDate,
            order.CancelledUserId,
            order.TerminationReason,
            order.TerminationDate,
            order.TerminationUserId,
            order.AuditTrail,
            order.Items,
            order.ItemComponents);

        var infraOrder = LabOrderModel.Load(
            "BLOCK-INFRA",
            order.EmrOrderId,
            order.OrderNo,
            order.OrderSource,
            order.LabOrderStatus,
            order.LastBillingReleaseStatus,
            order.OwareStatus,
            order.Patient,
            order.ExecutionRegId,
            order.DeferredInfo,
            order.BillingTindakanId,
            order.BillingLastError,
            order.CollectionInfo,
            order.LastBillingReleaseCheckAt,
            order.LastBillingReleaseCheckUserId,
            order.LastBillingReleaseMessage,
            order.ReleasedDate,
            order.ReleasedUserId,
            order.ReleaseNote,
            order.CancelledReason,
            order.CancelledDate,
            order.CancelledUserId,
            order.TerminationReason,
            order.TerminationDate,
            order.TerminationUserId,
            order.AuditTrail,
            order.Items,
            order.ItemComponents);

        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(infraOrder));

        var handler = new LabOrderReleaseHandler(_repo.Object, _billing);
        var act = async () => await handler.Handle(
            new LabOrderReleaseCmd(infraOrder.OrderId, "REL1", ""),
            CancellationToken.None);

        await act.Should().ThrowAsync<LabBillingReleaseValidationException>();
        _repo.Verify(x => x.SaveChanges(It.IsAny<LabOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Release_AfterAmendment_RevalidatesBillingNotReusingStaleTrace()
    {
        var order = VerifiedOrder();
        order.RecordLastBillingReleaseValidation(
            BillingReleaseValidationStatusEnum.Clear,
            "stale",
            "OLD");
        order.ReturnToRecordedAfterResultAmendment("AMEND");
        order.MarkVerified("PATH2");

        order.LastBillingReleaseStatus.Should().Be(BillingReleaseValidationStatusEnum.NotChecked);
        order.ReleasedUserId.Should().BeEmpty();

        _repo.Setup(x => x.LoadEntity(It.IsAny<ILabOrderKey>())).Returns(MayBe.From(order));

        var handler = new LabOrderReleaseHandler(_repo.Object, _billing);
        await handler.Handle(new LabOrderReleaseCmd(order.OrderId, "REL2", "ok"), CancellationToken.None);

        _repo.Verify(x => x.SaveChanges(It.Is<LabOrderModel>(o =>
            o.LabOrderStatus == LabOrderStatusEnum.Released
            && o.LastBillingReleaseCheckUserId == "REL2")), Times.Once);
    }
}

using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadJadwalFeature;

public class RadJadwalCancelHandlerTest
{
    private readonly Mock<IRadJadwalRepo> _jadwalRepoMock = new();
    private readonly Mock<IRadOrderRepo> _orderRepoMock = new();
    private readonly Mock<IRadTrackerIntegration> _trackerMock = new();
    private readonly DateTime _now = new(2026, 11, 26, 15, 0, 0);
    private readonly RadJadwalCancelHandler _sut;

    public RadJadwalCancelHandlerTest()
    {
        _trackerMock
            .Setup(t => t.PublishMilestoneAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new RadJadwalCancelHandler(
            _jadwalRepoMock.Object,
            _orderRepoMock.Object,
            new TestTglJamProvider(_now),
            _trackerMock.Object);
    }

    private static RadJadwalModel CreateScheduledJadwal(
        string jadwalRadId = "JAD0000000001",
        string orderId = "RDO0000000001")
        => RadJadwalModel.Create(
            orderId: orderId,
            orderNo: "RAD26110001",
            patient: new PatientSnapshotType("REG001", "MR0001", "Pasien Uji", new DateTime(1990, 1, 15), "L", 36),
            modality: RadModalityEnum.CT,
            scheduleNo: "SCH26110001",
            equipmentId: "EQ-CT01",
            equipmentName: "CT Scan 64 Slice",
            roomId: "RM-CT01",
            roomName: "Ruang CT 1",
            scheduleDate: new DateOnly(2026, 11, 27),
            startTime: new TimeOnly(9, 0),
            durationMinutes: 20,
            audit: new AuditInfoType("RAD_ADMIN", new DateTime(2026, 11, 26, 8, 0, 0)),
            jadwalRadId: jadwalRadId);

    private static RadOrderModel CreateScheduledOrder(
        string orderId = "RDO0000000001",
        RadOrderStatusEnum status = RadOrderStatusEnum.Scheduled)
    {
        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-001",
            patient: new PatientSnapshotType("REG001", "MR0001", "Pasien Uji", new DateTime(1990, 1, 15), "L", 36),
            modality: RadModalityEnum.CT,
            items: [RadOrderItemModel.Create("TRF01", "CT Brain", RadModalityEnum.CT)],
            orderNo: "RAD26110001",
            audit: new AuditInfoType("DOK01", new DateTime(2026, 11, 26, 8, 0, 0)),
            orderId: orderId);

        if (status == RadOrderStatusEnum.Scheduled)
            order.Schedule("RAD_ADMIN", new DateTime(2026, 11, 26, 8, 10, 0));
        else if (status == RadOrderStatusEnum.Charged)
            order.MarkCharged("TIN001", "BILL01", new DateTime(2026, 11, 26, 8, 15, 0));

        return order;
    }

    [Fact]
    public async Task Handle_ValidCancel_CancelsJadwalUnschedulesOrderBackToRequestedAndPublishesMilestone()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal("JAD0000000001", "RDO0000000001");
        var order = CreateScheduledOrder("RDO0000000001", RadOrderStatusEnum.Scheduled);

        _jadwalRepoMock.Setup(r => r.LoadEntity(It.Is<IRadJadwalKey>(k => k.JadwalRadId == "JAD0000000001")))
            .Returns(MayBe.From(jadwal));
        _orderRepoMock.Setup(r => r.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == "RDO0000000001")))
            .Returns(MayBe.From(order));

        var cmd = new RadJadwalCancelCmd(
            JadwalRadId: "JAD0000000001",
            Category: RadCancellationCategoryEnum.MesinRusak,
            Reason: "Mesin CT mengalami gangguan pendingin",
            UserId: "RAD_SPV");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be((int)RadJadwalStatusEnum.Cancelled);
        result.CancellationCategory.Should().Be(nameof(RadCancellationCategoryEnum.MesinRusak));
        result.CancellationReason.Should().Be("Mesin CT mengalami gangguan pendingin");

        jadwal.Status.Should().Be(RadJadwalStatusEnum.Cancelled);
        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Requested);
        order.IsCancelled.Should().BeFalse();
        order.IsActive.Should().BeTrue();
        order.AuditTrail.Modified.UserId.Should().Be("RAD_SPV");
        order.AuditTrail.Modified.Timestamp.Should().Be(_now);

        _jadwalRepoMock.Verify(r => r.SaveChanges(jadwal), Times.Once);
        _orderRepoMock.Verify(r => r.SaveChanges(order), Times.Once);
        _trackerMock.Verify(t => t.PublishMilestoneAsync(
            "REG001",
            "JAD0000000001",
            "Cancelled",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLinkedOrderIsNotScheduled_CancelsJadwalWithoutSavingOrder()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal("JAD0000000001", "RDO0000000001");
        var chargedOrder = CreateScheduledOrder("RDO0000000001", RadOrderStatusEnum.Charged);

        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe.From(jadwal));
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(chargedOrder));

        var cmd = new RadJadwalCancelCmd(
            "JAD0000000001",
            RadCancellationCategoryEnum.PasienBatal,
            "Pasien pulang paksa",
            "RAD_SPV");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be((int)RadJadwalStatusEnum.Cancelled);
        chargedOrder.RadOrderStatus.Should().Be(RadOrderStatusEnum.Charged);
        _jadwalRepoMock.Verify(r => r.SaveChanges(jadwal), Times.Once);
        _orderRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenScheduleNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe<RadJadwalModel>.None);

        var cmd = new RadJadwalCancelCmd(
            "JAD-NONE",
            RadCancellationCategoryEnum.NoShow,
            "Pasien tidak hadir",
            "RAD_ADMIN");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*JAD-NONE*");
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
        _orderRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
    }
}

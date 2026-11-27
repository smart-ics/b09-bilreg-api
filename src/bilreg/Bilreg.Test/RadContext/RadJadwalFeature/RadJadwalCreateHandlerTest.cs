using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadJadwalFeature;

public class RadJadwalCreateHandlerTest
{
    private readonly Mock<IRadJadwalRepo> _jadwalRepoMock = new();
    private readonly Mock<IRadJadwalDal> _jadwalDalMock = new();
    private readonly Mock<IRadEquipmentDal> _equipmentDalMock = new();
    private readonly Mock<IRadOrderRepo> _orderRepoMock = new();
    private readonly Mock<ISequencer> _sequencerMock = new();
    private readonly Mock<IRadTrackerIntegration> _trackerMock = new();
    private readonly DateTime _now = new(2026, 11, 26, 9, 30, 0);
    private readonly RadJadwalCreateHandler _sut;

    public RadJadwalCreateHandlerTest()
    {
        _sequencerMock
            .Setup(s => s.GetNextNoUrut(RadJadwalModel.ScheduleNoPrefix))
            .Returns(1);

        _jadwalDalMock
            .Setup(d => d.IsSlotOverlapping(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>()))
            .Returns(false);

        _trackerMock
            .Setup(t => t.PublishMilestoneAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new RadJadwalCreateHandler(
            _jadwalRepoMock.Object,
            _jadwalDalMock.Object,
            _equipmentDalMock.Object,
            _orderRepoMock.Object,
            _sequencerMock.Object,
            new TestTglJamProvider(_now),
            _trackerMock.Object);
    }

    private static RadOrderModel CreateTestOrder(
        string orderId = "RDO0000000001",
        RadModalityEnum modality = RadModalityEnum.CT,
        RadOrderStatusEnum status = RadOrderStatusEnum.Requested,
        int itemCount = 1)
    {
        var items = Enumerable.Range(1, itemCount)
            .Select(i => RadOrderItemModel.Create($"TRF0{i}", $"Pemeriksaan {i}", modality, RadUrgencyEnum.Rutin))
            .ToList();

        var order = RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-001",
            patient: new PatientSnapshotType("REG001", "MR0001", "Pasien Uji", new DateTime(1990, 1, 15), "L", 36),
            modality: modality,
            items: items,
            orderNo: "RAD26110001",
            audit: new AuditInfoType("DOK01", new DateTime(2026, 11, 26, 8, 0, 0)),
            orderId: orderId);

        if (status == RadOrderStatusEnum.Scheduled)
            order.Schedule("TECH01", new DateTime(2026, 11, 26, 8, 30, 0));
        else if (status == RadOrderStatusEnum.Charged)
            order.MarkCharged("TIN001", "BILL01", new DateTime(2026, 11, 26, 8, 45, 0));
        else if (status == RadOrderStatusEnum.Cancelled)
            order.Cancel("Dibatalkan dokter", "DOK01", new DateTime(2026, 11, 26, 8, 50, 0));

        return order;
    }

    private static RadEquipmentDto CreateEquipment(
        string equipmentId = "EQ-CT01",
        string modality = "CT",
        bool isActive = true) => new(
        EquipmentId: equipmentId,
        EquipmentName: "CT Scan 64 Slice",
        Modality: modality,
        RoomId: "RM-CT01",
        RoomName: "Ruang CT 1",
        IsActive: isActive);

    private static RadJadwalCreateCmd CreateValidCmd(
        string orderId = "RDO0000000001",
        string equipmentId = "EQ-CT01",
        DateOnly? scheduleDate = null,
        TimeOnly? startTime = null,
        int? durationMinutes = null) => new(
        OrderId: orderId,
        EquipmentId: equipmentId,
        ScheduleDate: scheduleDate ?? new DateOnly(2026, 11, 27),
        StartTime: startTime ?? new TimeOnly(10, 0),
        UserId: "RAD_ADMIN",
        DurationMinutes: durationMinutes,
        DoctorRadiologyId: "DR-RAD01",
        DoctorRadiologyName: "dr. Radiolog, Sp.Rad",
        PreparationNotes: "Puasa 4 jam",
        LabRequirements: "Kreatinin");

    [Fact]
    public async Task Handle_ValidRequestedOrder_SchedulesOrderSavesBothAndPublishesMilestone()
    {
        // Arrange
        var order = CreateTestOrder(modality: RadModalityEnum.CT, itemCount: 1);
        _orderRepoMock.Setup(r => r.LoadEntity(It.Is<IRadOrderKey>(k => k.OrderId == order.OrderId)))
            .Returns(MayBe.From(order));
        _equipmentDalMock.Setup(d => d.GetData("EQ-CT01"))
            .Returns(CreateEquipment("EQ-CT01", "CT", isActive: true));

        RadJadwalModel? savedJadwal = null;
        _jadwalRepoMock.Setup(r => r.SaveChanges(It.IsAny<RadJadwalModel>()))
            .Callback<RadJadwalModel>(j => savedJadwal = j);

        var cmd = CreateValidCmd();

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be("RDO0000000001");
        result.ScheduleNo.Should().Be("SCH2611000001");
        result.EquipmentId.Should().Be("EQ-CT01");
        result.DurationMinutes.Should().Be(20); // default CT 1 item
        result.TurnaroundBufferMinutes.Should().Be(10); // default CT buffer
        result.StartTime.Should().Be(new TimeSpan(10, 0, 0));
        result.EndTime.Should().Be(new TimeSpan(10, 30, 0));
        result.Status.Should().Be((int)RadJadwalStatusEnum.Scheduled);

        order.RadOrderStatus.Should().Be(RadOrderStatusEnum.Scheduled);
        savedJadwal.Should().NotBeNull();
        savedJadwal!.JadwalRadId.Should().StartWith(RadJadwalModel.IdPrefix);

        _jadwalDalMock.Verify(d => d.IsSlotOverlapping(
            "EQ-CT01",
            new DateTime(2026, 11, 27),
            new TimeSpan(10, 0, 0),
            new TimeSpan(10, 30, 0),
            null), Times.Once);

        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Once);
        _orderRepoMock.Verify(r => r.SaveChanges(order), Times.Once);
        _trackerMock.Verify(t => t.PublishMilestoneAsync(
            "REG001",
            savedJadwal.JadwalRadId,
            "Scheduled",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MrOrderWithMriEquipmentAlias_Uses15MinuteBufferAndNormalizesModality()
    {
        // Arrange
        var order = CreateTestOrder(modality: RadModalityEnum.MR, itemCount: 1);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));
        _equipmentDalMock.Setup(d => d.GetData("EQ-MRI01"))
            .Returns(CreateEquipment("EQ-MRI01", "MRI", isActive: true));

        var cmd = CreateValidCmd(equipmentId: "EQ-MRI01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Modality.Should().Be("MR");
        result.DurationMinutes.Should().Be(30); // default MR 1 item
        result.TurnaroundBufferMinutes.Should().Be(15);
        result.EndTime.Should().Be(new TimeSpan(10, 45, 0));
    }

    [Fact]
    public async Task Handle_WhenSlotOverlaps_ThrowsRadJadwalSlotConflictExceptionAndDoesNotSave()
    {
        // Arrange
        var order = CreateTestOrder(modality: RadModalityEnum.CT);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));
        _equipmentDalMock.Setup(d => d.GetData("EQ-CT01"))
            .Returns(CreateEquipment("EQ-CT01", "CT", isActive: true));
        _jadwalDalMock
            .Setup(d => d.IsSlotOverlapping(
                "EQ-CT01",
                new DateTime(2026, 11, 27),
                new TimeSpan(10, 0, 0),
                new TimeSpan(10, 30, 0),
                null))
            .Returns(true);

        var cmd = CreateValidCmd();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RadJadwalSlotConflictException>()
            .WithMessage("*sudah terisi*");

        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
        _orderRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadOrderModel>()), Times.Never);
        _trackerMock.Verify(t => t.PublishMilestoneAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEquipmentNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var order = CreateTestOrder(modality: RadModalityEnum.CT);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));
        _equipmentDalMock.Setup(d => d.GetData("EQ-MISSING"))
            .Returns((RadEquipmentDto?)null);

        var cmd = CreateValidCmd(equipmentId: "EQ-MISSING");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*EQ-MISSING*");
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEquipmentInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder(modality: RadModalityEnum.CT);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));
        _equipmentDalMock.Setup(d => d.GetData("EQ-CT01"))
            .Returns(CreateEquipment("EQ-CT01", "CT", isActive: false));

        var cmd = CreateValidCmd();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak aktif*");
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEquipmentModalityMismatchesOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder(modality: RadModalityEnum.MR);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));
        _equipmentDalMock.Setup(d => d.GetData("EQ-CT01"))
            .Returns(CreateEquipment("EQ-CT01", "CT", isActive: true));

        var cmd = CreateValidCmd(equipmentId: "EQ-CT01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak sesuai dengan modalitas*");
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
    }

    [Theory]
    [InlineData(RadOrderStatusEnum.Scheduled)]
    [InlineData(RadOrderStatusEnum.Charged)]
    [InlineData(RadOrderStatusEnum.Cancelled)]
    public async Task Handle_WhenOrderStatusNotRequested_ThrowsInvalidOperationException(RadOrderStatusEnum status)
    {
        // Arrange
        var order = CreateTestOrder(status: status);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));

        var cmd = CreateValidCmd();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderVoided_ThrowsInvalidOperationException()
    {
        // Arrange
        var order = CreateTestOrder(status: RadOrderStatusEnum.Requested);
        order.AuditTrail.Batal("ADMIN", _now);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));

        var cmd = CreateValidCmd();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan atau void*");
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe<RadOrderModel>.None);

        var cmd = CreateValidCmd(orderId: "RDO-NONE");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*RDO-NONE*");
    }

    [Fact]
    public async Task Handle_WhenTrackerThrowsException_StillSucceedsResiliently()
    {
        // Arrange
        var order = CreateTestOrder(modality: RadModalityEnum.US);
        _orderRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadOrderKey>()))
            .Returns(MayBe.From(order));
        _equipmentDalMock.Setup(d => d.GetData("EQ-USG01"))
            .Returns(CreateEquipment("EQ-USG01", "USG", isActive: true));
        _trackerMock
            .Setup(t => t.PublishMilestoneAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tracker timeout"));

        var cmd = CreateValidCmd(equipmentId: "EQ-USG01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be((int)RadJadwalStatusEnum.Scheduled);
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Once);
        _orderRepoMock.Verify(r => r.SaveChanges(order), Times.Once);
    }
}

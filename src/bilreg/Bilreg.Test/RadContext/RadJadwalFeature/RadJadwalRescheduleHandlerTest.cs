using Bilreg.Application.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadJadwalFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadJadwalFeature;

public class RadJadwalRescheduleHandlerTest
{
    private readonly Mock<IRadJadwalRepo> _jadwalRepoMock = new();
    private readonly Mock<IRadJadwalDal> _jadwalDalMock = new();
    private readonly Mock<IRadEquipmentDal> _equipmentDalMock = new();
    private readonly Mock<IRadTrackerIntegration> _trackerMock = new();
    private readonly DateTime _now = new(2026, 11, 26, 14, 0, 0);
    private readonly RadJadwalRescheduleHandler _sut;

    public RadJadwalRescheduleHandlerTest()
    {
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

        _sut = new RadJadwalRescheduleHandler(
            _jadwalRepoMock.Object,
            _jadwalDalMock.Object,
            _equipmentDalMock.Object,
            new TestTglJamProvider(_now),
            _trackerMock.Object);
    }

    private static RadJadwalModel CreateScheduledJadwal(
        string jadwalRadId = "JAD0000000001",
        RadModalityEnum modality = RadModalityEnum.CT)
        => RadJadwalModel.Create(
            orderId: "RDO0000000001",
            orderNo: "RAD26110001",
            patient: new PatientSnapshotType("REG001", "MR0001", "Pasien Uji", new DateTime(1990, 1, 15), "L", 36),
            modality: modality,
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

    private static RadEquipmentDto CreateEquipment(
        string equipmentId = "EQ-CT02",
        string equipmentName = "CT Scan 128 Slice",
        string modality = "CT",
        bool isActive = true) => new(
        EquipmentId: equipmentId,
        EquipmentName: equipmentName,
        Modality: modality,
        RoomId: "RM-CT02",
        RoomName: "Ruang CT 2",
        IsActive: isActive);

    [Fact]
    public async Task Handle_ValidReschedule_UpdatesInPlaceCapturesHistorySnapshotAndPublishesMilestone()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal("JAD0000000001");
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.Is<IRadJadwalKey>(k => k.JadwalRadId == "JAD0000000001")))
            .Returns(MayBe.From(jadwal));
        _equipmentDalMock.Setup(d => d.GetData("EQ-CT02"))
            .Returns(CreateEquipment("EQ-CT02", "CT Scan 128 Slice", "CT", true));

        RadJadwalModel? savedModel = null;
        RadJadwalRescheduleDto? savedSnapshot = null;
        _jadwalRepoMock
            .Setup(r => r.SaveReschedule(It.IsAny<RadJadwalModel>(), It.IsAny<RadJadwalRescheduleDto>()))
            .Callback<RadJadwalModel, RadJadwalRescheduleDto>((m, s) =>
            {
                savedModel = m;
                savedSnapshot = s;
            });

        var cmd = new RadJadwalRescheduleCmd(
            JadwalRadId: "JAD0000000001",
            NewEquipmentId: "EQ-CT02",
            NewScheduleDate: new DateOnly(2026, 11, 28),
            NewStartTime: new TimeOnly(11, 0),
            NewEndTime: new TimeOnly(11, 45),
            Reason: "Maintenance mesin CT 1",
            UserId: "RAD_SPV");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert - In-place update preserves identity & ScheduleNo
        result.JadwalRadId.Should().Be("JAD0000000001");
        result.ScheduleNo.Should().Be("SCH26110001");
        result.EquipmentId.Should().Be("EQ-CT02");
        result.EquipmentName.Should().Be("CT Scan 128 Slice");
        result.RoomId.Should().Be("RM-CT02");
        result.ScheduleDate.Should().Be(new DateTime(2026, 11, 28));
        result.StartTime.Should().Be(new TimeSpan(11, 0, 0));
        result.EndTime.Should().Be(new TimeSpan(11, 45, 0));
        result.RescheduleCount.Should().Be(1);

        // Assert - Collision check excludes self
        _jadwalDalMock.Verify(d => d.IsSlotOverlapping(
            "EQ-CT02",
            new DateTime(2026, 11, 28),
            new TimeSpan(11, 0, 0),
            new TimeSpan(11, 45, 0),
            "JAD0000000001"), Times.Once);

        // Assert - Reschedule history snapshot captures OLD state
        savedModel.Should().BeSameAs(jadwal);
        savedSnapshot.Should().NotBeNull();
        savedSnapshot!.RescheduleId.Should().StartWith("RSC");
        savedSnapshot.JadwalRadId.Should().Be("JAD0000000001");
        savedSnapshot.RescheduleIndex.Should().Be(1);
        savedSnapshot.OldScheduleDate.Should().Be(new DateTime(2026, 11, 27));
        savedSnapshot.OldStartTime.Should().Be(new TimeSpan(9, 0, 0));
        savedSnapshot.OldEndTime.Should().Be(new TimeSpan(9, 30, 0));
        savedSnapshot.OldEquipmentId.Should().Be("EQ-CT01");
        savedSnapshot.OldEquipmentName.Should().Be("CT Scan 64 Slice");
        savedSnapshot.Reason.Should().Be("Maintenance mesin CT 1");
        savedSnapshot.RescheduledBy.Should().Be("RAD_SPV");
        savedSnapshot.RescheduledAt.Should().Be(_now);

        _trackerMock.Verify(t => t.PublishMilestoneAsync(
            "REG001",
            "JAD0000000001",
            "Rescheduled",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNewSlotConflicts_PassesExcludeJadwalIdAndThrowsSlotConflictException()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal("JAD0000000001");
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe.From(jadwal));
        _equipmentDalMock.Setup(d => d.GetData("EQ-CT01"))
            .Returns(CreateEquipment("EQ-CT01", "CT Scan 64 Slice", "CT", true));
        _jadwalDalMock
            .Setup(d => d.IsSlotOverlapping(
                "EQ-CT01",
                new DateTime(2026, 11, 27),
                new TimeSpan(9, 15, 0),
                new TimeSpan(9, 45, 0),
                "JAD0000000001"))
            .Returns(true);

        var cmd = new RadJadwalRescheduleCmd(
            "JAD0000000001",
            "EQ-CT01",
            new DateOnly(2026, 11, 27),
            new TimeOnly(9, 15),
            new TimeOnly(9, 45),
            "Geser jam pasien",
            "RAD_SPV");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RadJadwalSlotConflictException>()
            .WithMessage("*sudah terisi*");

        _jadwalDalMock.Verify(d => d.IsSlotOverlapping(
            "EQ-CT01",
            new DateTime(2026, 11, 27),
            new TimeSpan(9, 15, 0),
            new TimeSpan(9, 45, 0),
            "JAD0000000001"), Times.Once);
        _jadwalRepoMock.Verify(r => r.SaveReschedule(It.IsAny<RadJadwalModel>(), It.IsAny<RadJadwalRescheduleDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenScheduleNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe<RadJadwalModel>.None);

        var cmd = new RadJadwalRescheduleCmd(
            "JAD-MISSING",
            "EQ-CT01",
            new DateOnly(2026, 11, 28),
            new TimeOnly(10, 0),
            new TimeOnly(10, 30),
            "Ubah jadwal",
            "RAD_SPV");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*JAD-MISSING*");
    }

    [Fact]
    public async Task Handle_WhenScheduleNotScheduled_ThrowsInvalidOperationException()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal();
        jadwal.Cancel(RadCancellationCategoryEnum.PasienBatal, "Pasien batal", "U1");
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe.From(jadwal));

        var cmd = new RadJadwalRescheduleCmd(
            jadwal.JadwalRadId,
            "EQ-CT01",
            new DateOnly(2026, 11, 28),
            new TimeOnly(10, 0),
            new TimeOnly(10, 30),
            "Coba reschedule",
            "RAD_SPV");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status Scheduled*");
        _jadwalRepoMock.Verify(r => r.SaveReschedule(It.IsAny<RadJadwalModel>(), It.IsAny<RadJadwalRescheduleDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEquipmentMismatchedOrInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal(modality: RadModalityEnum.CT);
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe.From(jadwal));
        _equipmentDalMock.Setup(d => d.GetData("EQ-MRI01"))
            .Returns(CreateEquipment("EQ-MRI01", "MRI 1.5T", "MRI", true));

        var cmd = new RadJadwalRescheduleCmd(
            jadwal.JadwalRadId,
            "EQ-MRI01",
            new DateOnly(2026, 11, 28),
            new TimeOnly(10, 0),
            new TimeOnly(10, 45),
            "Salah pilih mesin",
            "RAD_SPV");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak sesuai dengan modalitas*");
        _jadwalRepoMock.Verify(r => r.SaveReschedule(It.IsAny<RadJadwalModel>(), It.IsAny<RadJadwalRescheduleDto>()), Times.Never);
    }
}

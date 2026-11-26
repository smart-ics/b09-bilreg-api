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

public class RadJadwalClearanceHandlerTest
{
    private readonly Mock<IRadJadwalRepo> _jadwalRepoMock = new();
    private readonly Mock<IRadTrackerIntegration> _trackerMock = new();
    private readonly DateTime _now = new(2026, 11, 27, 8, 50, 0);
    private readonly RadJadwalClearanceHandler _sut;

    public RadJadwalClearanceHandlerTest()
    {
        _trackerMock
            .Setup(t => t.PublishMilestoneAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new RadJadwalClearanceHandler(
            _jadwalRepoMock.Object,
            new TestTglJamProvider(_now),
            _trackerMock.Object);
    }

    private static RadJadwalModel CreateScheduledJadwal(string jadwalRadId = "JAD0000000001")
        => RadJadwalModel.Create(
            orderId: "RDO0000000001",
            orderNo: "RAD26110001",
            patient: new PatientSnapshotType("REG001", "MR0001", "Pasien Uji", new DateTime(1990, 1, 15), "L", 36),
            modality: RadModalityEnum.MR,
            scheduleNo: "SCH26110001",
            equipmentId: "EQ-MRI01",
            equipmentName: "MRI 1.5 Tesla",
            roomId: "RM-MRI01",
            roomName: "Ruang MRI 1",
            scheduleDate: new DateOnly(2026, 11, 27),
            startTime: new TimeOnly(9, 0),
            durationMinutes: 30,
            audit: new AuditInfoType("RAD_ADMIN", new DateTime(2026, 11, 26, 8, 0, 0)),
            jadwalRadId: jadwalRadId);

    [Fact]
    public async Task Handle_ArrivedAndCleared_CompletesScheduleSavesAndPublishesCompletedMilestone()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal();
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.Is<IRadJadwalKey>(k => k.JadwalRadId == "JAD0000000001")))
            .Returns(MayBe.From(jadwal));

        var checklist = new RadSafetyChecklistType(
            HasMetalImplant: false,
            HasContrastAllergy: false,
            IsPregnant: false,
            Notes: "Bebas logam, eGFR normal");

        var cmd = new RadJadwalClearanceCmd(
            JadwalRadId: "JAD0000000001",
            IsArrived: true,
            IsCleared: true,
            ClearanceNotes: "Siap masuk ruang MRI",
            UserId: "NS_RAD",
            SafetyChecklist: checklist);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsArrived.Should().BeTrue();
        result.ArrivedAt.Should().Be(_now);
        result.IsCleared.Should().BeTrue();
        result.ClearanceNotes.Should().Be("Siap masuk ruang MRI");
        result.ClearedUserId.Should().Be("NS_RAD");
        result.ClearedAt.Should().Be(_now);
        result.Status.Should().Be((int)RadJadwalStatusEnum.Completed);

        jadwal.Status.Should().Be(RadJadwalStatusEnum.Completed);
        jadwal.SafetyChecklist.Should().Be(checklist);

        _jadwalRepoMock.Verify(r => r.SaveChanges(jadwal), Times.Once);
        _trackerMock.Verify(t => t.PublishMilestoneAsync(
            "REG001",
            "JAD0000000001",
            "Completed",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ArrivedButNotCleared_KeepsStatusScheduledAndDoesNotPublishCompletedMilestone()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal();
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe.From(jadwal));

        var checklist = new RadSafetyChecklistType(
            HasMetalImplant: true,
            Notes: "Implan koklea belum ada surat aman MRI");

        var cmd = new RadJadwalClearanceCmd(
            JadwalRadId: "JAD0000000001",
            IsArrived: true,
            IsCleared: false,
            ClearanceNotes: "Menunggu konfirmasi dokter THT",
            UserId: "NS_RAD",
            SafetyChecklist: checklist);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsArrived.Should().BeTrue();
        result.IsCleared.Should().BeFalse();
        result.Status.Should().Be((int)RadJadwalStatusEnum.Scheduled);
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);

        _jadwalRepoMock.Verify(r => r.SaveChanges(jadwal), Times.Once);
        _trackerMock.Verify(t => t.PublishMilestoneAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ClearedButNotArrived_KeepsStatusScheduledAndDoesNotPublishCompletedMilestone()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal();
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe.From(jadwal));

        var cmd = new RadJadwalClearanceCmd(
            JadwalRadId: "JAD0000000001",
            IsArrived: false,
            IsCleared: true,
            ClearanceNotes: "Pra-skrining telepon aman, pasien belum hadir",
            UserId: "NS_RAD");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsArrived.Should().BeFalse();
        result.IsCleared.Should().BeTrue();
        result.Status.Should().Be((int)RadJadwalStatusEnum.Scheduled);
        jadwal.Status.Should().Be(RadJadwalStatusEnum.Scheduled);

        _jadwalRepoMock.Verify(r => r.SaveChanges(jadwal), Times.Once);
        _trackerMock.Verify(t => t.PublishMilestoneAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyArrivedBefore_PreservesOriginalArrivedAtAndCompletesOnClearance()
    {
        // Arrange
        var jadwal = CreateScheduledJadwal();
        var earlierArrival = new DateTime(2026, 11, 27, 8, 30, 0);
        jadwal.RecordArrival("FO_RAD", earlierArrival);

        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe.From(jadwal));

        var cmd = new RadJadwalClearanceCmd(
            JadwalRadId: "JAD0000000001",
            IsArrived: true,
            IsCleared: true,
            ClearanceNotes: "Terverifikasi perawat",
            UserId: "NS_RAD");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ArrivedAt.Should().Be(earlierArrival);
        result.ClearedAt.Should().Be(_now);
        result.Status.Should().Be((int)RadJadwalStatusEnum.Completed);
    }

    [Fact]
    public async Task Handle_WhenScheduleNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _jadwalRepoMock.Setup(r => r.LoadEntity(It.IsAny<IRadJadwalKey>()))
            .Returns(MayBe<RadJadwalModel>.None);

        var cmd = new RadJadwalClearanceCmd("JAD-404", true, true, "OK", "NS_RAD");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*JAD-404*");
        _jadwalRepoMock.Verify(r => r.SaveChanges(It.IsAny<RadJadwalModel>()), Times.Never);
    }
}

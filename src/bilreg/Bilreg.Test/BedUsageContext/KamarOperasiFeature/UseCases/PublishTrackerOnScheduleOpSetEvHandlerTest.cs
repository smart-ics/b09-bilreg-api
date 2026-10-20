using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class PublishTrackerOnScheduleOpSetEvHandlerTest
{
    private readonly Mock<IPasienTrackerRepo> _trackerRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private PublishTrackerOnScheduleOpSetEvHandler CreateSut()
    {
        return new PublishTrackerOnScheduleOpSetEvHandler(
            _trackerRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static OkScheduleOpSetEvent CreateSampleEvent(
        string orderOpId = "ORD-001",
        string regId = "REG-001",
        string scheduleOpId = "SCH-001")
    {
        var cmd = new OkScheduleOpSetCommand(orderOpId, "KAMAR-1", "2026-10-20", "08:00", 60, "u1");
        var schedule = new ScheduleOpModel(
            scheduleOpId,
            new DateTime(2026, 10, 16),
            AuditTrailType.Create("u1", new DateTime(2026, 10, 16)),
            new OrderOpReff(orderOpId, new DateTime(2026, 10, 16), "Herniotomi"),
            new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            UrgencyLevelEnum.Elective,
            60,
            new DateTime(2026, 10, 20),
            new KamarReff("KAMAR-1", "OK 1"),
            new RegReff(regId, "PAS-001", "Pasien Test"),
            new PpaReff("DOC-01", "dr. Bedah"),
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 9, 0, 0),
            OpCaseStateEnum.Scheduled,
            []);

        return new OkScheduleOpSetEvent(cmd, schedule);
    }

    [Fact]
    public async Task Handle_WhenRegIdEmpty_ShouldNotCallSaveChanges()
    {
        // Arrange
        var ev = CreateSampleEvent(regId: "");
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        _trackerRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTrackerFoundDirectlyByRegId_ShouldAppendMilestoneAndSaveChanges()
    {
        // Arrange
        var testNow = new DateTime(2026, 10, 20, 8, 30, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(testNow);

        var person = new PersonType("Pasien Test", new DateOnly(1990, 1, 1));
        var tracker = new PasienTrackerModel(
            "TRK-001",
            person,
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new List<PasienTrackerEventType>
            {
                new(1, "REGISTER", new DateTime(2026, 10, 20, 7, 0, 0), "REG-001")
            });

        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "REG-001")))
            .Returns(MayBe.From(tracker));

        PasienTrackerModel? savedTracker = null;
        _trackerRepoMock.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(t => savedTracker = t);

        var ev = CreateSampleEvent(regId: "REG-001", scheduleOpId: "SCH-001");
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        savedTracker.Should().NotBeNull();
        savedTracker!.ListEvent.Should().ContainSingle(e =>
            e.EventName == "OPERASI_TERJADWAL" &&
            e.ReffId == "SCH-001" &&
            e.EventDate == testNow);
        _trackerRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTrackerFoundViaListDataFallback_ShouldAppendMilestoneAndSaveChanges()
    {
        // Arrange
        var testNow = new DateTime(2026, 10, 20, 9, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(testNow);

        // Direct lookup returns None
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "REG-001")))
            .Returns(MayBe<PasienTrackerModel>.None);

        var person = new PersonType("Pasien Test", new DateOnly(1990, 1, 1));
        var trackerHeader = new PasienTrackerView(
            "TRK-002",
            person,
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20));

        var trackerModel = new PasienTrackerModel(
            "TRK-002",
            person,
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new List<PasienTrackerEventType>
            {
                new(1, "REGISTER", new DateTime(2026, 10, 20, 7, 30, 0), "REG-001")
            });

        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), new DateOnly(1990, 1, 1)))
            .Returns(new List<PasienTrackerView> { trackerHeader });
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "TRK-002")))
            .Returns(MayBe.From(trackerModel));

        PasienTrackerModel? savedTracker = null;
        _trackerRepoMock.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(t => savedTracker = t);

        var ev = CreateSampleEvent(regId: "REG-001", scheduleOpId: "SCH-002");
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        savedTracker.Should().NotBeNull();
        savedTracker!.ListEvent.Should().ContainSingle(e =>
            e.EventName == "OPERASI_TERJADWAL" &&
            e.ReffId == "SCH-002" &&
            e.EventDate == testNow);
        _trackerRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTrackerNotFound_ShouldNotCallSaveChanges()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 9, 0, 0));
        _trackerRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe<PasienTrackerModel>.None);
        _trackerRepoMock.Setup(x => x.ListData(It.IsAny<Periode>(), It.IsAny<DateOnly>()))
            .Returns(new List<PasienTrackerView>());

        var ev = CreateSampleEvent(regId: "REG-999");
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        _trackerRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Never);
    }
}

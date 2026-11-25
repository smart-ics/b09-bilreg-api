using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class PublishTrackerOnPreOpClearedEvHandlerTest
{
    private readonly Mock<IPasienTrackerRepo> _trackerRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private PublishTrackerOnPreOpClearedEvHandler CreateSut()
    {
        return new PublishTrackerOnPreOpClearedEvHandler(
            _trackerRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static OkPreOpClearedEvent CreateSampleEvent(
        string clearanceId = "CLR-001",
        string regId = "REG-001",
        string scheduleOpId = "SCH-001",
        string orderOpId = "ORD-001",
        PasienReff? pasien = null,
        DateTime? tglOp = null)
    {
        var occurredAt = new DateTime(2026, 10, 20, 8, 30, 0);
        var clearance = new PreOpClearanceModel(
            clearanceId,
            scheduleOpId,
            orderOpId,
            regId,
            1,
            PreOpClearanceStatusEnum.Cleared,
            [new PreOpChecklistItemType(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "OK")],
            "PPA-VER",
            "PPA-DOK",
            occurredAt,
            string.Empty,
            string.Empty,
            string.Empty,
            AuditTrailType.Create("u1", occurredAt));

        return new OkPreOpClearedEvent(
            clearance,
            occurredAt,
            pasien ?? new PasienReff("PAS-001", "Pasien Test", new DateOnly(1990, 1, 1), "L"),
            tglOp ?? new DateTime(2026, 10, 20));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenRegIdEmpty_ShouldNotCallRepoOrSaveChanges(string regId)
    {
        // Arrange
        var ev = CreateSampleEvent(regId: regId);
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        _trackerRepoMock.Verify(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()), Times.Never);
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

        var ev = CreateSampleEvent(clearanceId: "CLR-001", regId: "REG-001");
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        savedTracker.Should().NotBeNull();
        savedTracker!.ListEvent.Should().ContainSingle(e =>
            e.EventName == "PREOP_CLEARED" &&
            e.ReffId == "CLR-001" &&
            e.EventDate == testNow);
        _trackerRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTrackerFoundViaListDataFallback_ShouldAppendMilestoneAndSaveChanges()
    {
        // Arrange
        var testNow = new DateTime(2026, 10, 20, 9, 0, 0);
        var tglOp = new DateTime(2026, 10, 21, 10, 0, 0);
        var tglLahir = new DateOnly(1990, 1, 1);
        _tglJamProviderMock.Setup(x => x.Now).Returns(testNow);

        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "REG-001")))
            .Returns(MayBe<PasienTrackerModel>.None);

        var person = new PersonType("Pasien Test", tglLahir);
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

        _trackerRepoMock.Setup(x => x.ListData(
                It.Is<Periode>(p => p.Tgl1.Date == tglOp.Date && p.Tgl2.Date == tglOp.Date),
                tglLahir))
            .Returns(new List<PasienTrackerView> { trackerHeader });
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "TRK-002")))
            .Returns(MayBe.From(trackerModel));

        PasienTrackerModel? savedTracker = null;
        _trackerRepoMock.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(t => savedTracker = t);

        var ev = CreateSampleEvent(
            clearanceId: "CLR-002",
            regId: "REG-001",
            pasien: new PasienReff("PAS-001", "Pasien Test", tglLahir, "L"),
            tglOp: tglOp);
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        savedTracker.Should().NotBeNull();
        savedTracker!.ListEvent.Should().ContainSingle(e =>
            e.EventName == "PREOP_CLEARED" &&
            e.ReffId == "CLR-002" &&
            e.EventDate == testNow);
        _trackerRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTrackerFoundViaFallbackWithoutTglOp_ShouldUseOccurredAtDateForPeriode()
    {
        // Arrange
        var testNow = new DateTime(2026, 10, 20, 9, 15, 0);
        var tglLahir = new DateOnly(1990, 1, 1);
        _tglJamProviderMock.Setup(x => x.Now).Returns(testNow);

        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "REG-001")))
            .Returns(MayBe<PasienTrackerModel>.None);

        var person = new PersonType("Pasien Test", tglLahir);
        var trackerHeader = new PasienTrackerView(
            "TRK-003",
            person,
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20));

        var trackerModel = new PasienTrackerModel(
            "TRK-003",
            person,
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new DateOnly(2026, 10, 20),
            new List<PasienTrackerEventType>
            {
                new(1, "REGISTER", new DateTime(2026, 10, 20, 7, 30, 0), "REG-001")
            });

        _trackerRepoMock.Setup(x => x.ListData(
                It.Is<Periode>(p => p.Tgl1.Date == testNow.Date && p.Tgl2.Date == testNow.Date),
                tglLahir))
            .Returns(new List<PasienTrackerView> { trackerHeader });
        _trackerRepoMock.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == "TRK-003")))
            .Returns(MayBe.From(trackerModel));

        var clearance = PreOpClearanceModel.Create("SCH-003", "ORD-003", "REG-001", 1, "PPA-V", "PPA-D", "u1", testNow);
        var ev = new OkPreOpClearedEvent(
            clearance,
            testNow,
            new PasienReff("PAS-001", "Pasien Test", tglLahir, "L"),
            null);
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        trackerModel.ListEvent.Should().ContainSingle(e =>
            e.EventName == "PREOP_CLEARED" &&
            e.ReffId == clearance.ClearanceId &&
            e.EventDate == testNow);
        _trackerRepoMock.Verify(x => x.SaveChanges(trackerModel), Times.Once);
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

    [Fact]
    public async Task Handle_WhenDirectLookupNotFoundAndPasienNull_ShouldNotCallListDataOrSaveChanges()
    {
        // Arrange
        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 20, 9, 0, 0));
        _trackerRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>()))
            .Returns(MayBe<PasienTrackerModel>.None);

        var clearance = PreOpClearanceModel.Create("SCH-004", "ORD-004", "REG-999", 1, "PPA-V", "PPA-D", "u1", new DateTime(2026, 10, 20));
        var ev = new OkPreOpClearedEvent(clearance, new DateTime(2026, 10, 20));
        var sut = CreateSut();

        // Act
        await sut.Handle(ev, CancellationToken.None);

        // Assert
        _trackerRepoMock.Verify(x => x.ListData(It.IsAny<Periode>(), It.IsAny<DateOnly>()), Times.Never);
        _trackerRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()), Times.Never);
    }
}

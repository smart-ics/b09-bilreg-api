using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedTransferCommandTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<ITipeKamarRepo> _tipeKamarRepoMock = new();
    private readonly Mock<IRoomRateRepo> _roomRateRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly PakaiBedTransferCommandHandler _sut;

    private readonly RegModel _defaultReg;
    private readonly BedType _defaultBedLama;
    private readonly BedType _defaultBedTarget;
    private readonly KamarType _defaultKamarLama;
    private readonly KamarType _defaultKamarTarget;
    private readonly LayananType _defaultLayanan;
    private readonly TipeKamarType _defaultTipeKamar;
    private readonly KelasType _defaultKelas;
    private readonly RoomRateRegulerType _defaultRoomRate;
    private readonly BedOperationalModel _targetOperationalReady;
    private readonly BedOperationalModel _oldOperational;

    public PakaiBedTransferCommandTest()
    {
        _sut = new PakaiBedTransferCommandHandler(
            _pakaiBedRepoMock.Object,
            _bedRepoMock.Object,
            _kamarRepoMock.Object,
            _kelasRepoMock.Object,
            _tipeKamarRepoMock.Object,
            _roomRateRepoMock.Object,
            _layananRepoMock.Object,
            _regRepoMock.Object,
            _bedOperationalRepoMock.Object,
            _tglJamProviderMock.Object);

        _defaultReg = RegModel.Default;
        _defaultKelas = KelasType.Default with { KelasId = "KLS01", KelasName = "Kelas 1" };

        _defaultKamarLama = KamarType.Default with
        {
            KamarId = "KMR01",
            KamarName = "Kamar Mawar 1",
            Kelas = new KelasReff("KLS01", "Kelas 1"),
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar")
        };

        _defaultKamarTarget = KamarType.Default with
        {
            KamarId = "KMR02",
            KamarName = "Kamar Mawar 2",
            Kelas = new KelasReff("KLS01", "Kelas 1"),
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar")
        };

        _defaultBedLama = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            IsAktif = true
        };

        _defaultBedTarget = BedType.Default with
        {
            BedId = "BED02",
            BedName = "Bed 02",
            Kamar = new KamarReff("KMR02", "Kamar Mawar 2"),
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            IsAktif = true
        };

        _defaultLayanan = LayananType.Default with { LayananId = "LYN01", LayananName = "Bangsal Mawar" };
        _defaultTipeKamar = TipeKamarType.Default with { TipeKamarId = "TK01", TipeKamarName = "Standard" };

        _defaultRoomRate = new RoomRateRegulerType(
            "KMR02",
            new KamarReff("KMR02", "Kamar Mawar 2"),
            new[]
            {
                new RoomRateRegulerTipeType(
                    _defaultTipeKamar.ToReff(),
                    new[] { new RoomRateKomponenType(new KomponenReff("KM01", "Sewa Kamar"), 250000m) })
            });

        // Set target bed operational to Ready
        var targetBaseOp = BedOperationalModel.Create(
            "BED02", "BGS01", "KMR02", OccupancyPolicyReff.Default);
        var readyTime = new DateTime(2026, 10, 24, 7, 0, 0, DateTimeKind.Utc);
        _targetOperationalReady = targetBaseOp.VerifyReady(
            "Pembersihan selesai", "", "ACT-01", "VER-01", readyTime, readyTime.AddMinutes(5), "FACT-01", "REQ-01");

        _oldOperational = BedOperationalModel.Create(
            "BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default);

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc));

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(_defaultReg));

        _bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(_defaultBedLama));
        _bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED02")))
            .Returns(MayBe.From(_defaultBedTarget));

        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KMR01")))
            .Returns(MayBe.From(_defaultKamarLama));
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KMR02")))
            .Returns(MayBe.From(_defaultKamarTarget));

        _kelasRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe.From(_defaultKelas));

        _tipeKamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITipeKamarKey>()))
            .Returns(MayBe.From(_defaultTipeKamar));

        _roomRateRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe.From<IRoomRate<IRoomRateDetail>>(_defaultRoomRate));

        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(_defaultLayanan));

        _pakaiBedRepoMock.Setup(x => x.GetActiveOccupant(It.IsAny<IBedKey>()))
            .Returns(MayBe<PakaiBedModel>.None);

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED02")))
            .Returns(MayBe.From(_targetOperationalReady));
        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(_oldOperational));
    }

    private static PakaiBedModel CreateActiveOldPakaiBed(string pakaiBedId = "PKB001")
    {
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff("REG001", "PAS001", "Budi");
        var layanan = new LayananReff("LYN01", "Bangsal Mawar");
        var bed = new BedReff("BED01", "Bed 01", true);
        var tipeKamar = new TipeKamarReff("TK01", "Standard", true);
        var kelas = new KelasReff("KLS01", "Kelas 1");
        var auditTrail = new AuditTrailType(masuk, AuditInfoType.Default, AuditInfoType.Default);

        return new PakaiBedModel(
            pakaiBedId,
            periode,
            reg,
            layanan,
            bed,
            tipeKamar,
            kelas,
            200000m,
            auditTrail);
    }

    [Fact]
    public async Task Handle_ValidRequest_ExecutesAtomicTransferSuccessfully()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        var transferTime = new DateTime(2026, 10, 24, 10, 30, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB001",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: transferTime,
            AlasanPindah: "Pindah atas permintaan pasien"
        );

        PakaiBedModel? savedOldBed = null;
        PakaiBedModel? savedNewBed = null;
        var savedOperationalModels = new List<BedOperationalModel>();

        _pakaiBedRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedModel>(model =>
            {
                if (model.PakaiBedId == "PKB001")
                    savedOldBed = model;
                else
                    savedNewBed = model;
            });

        _bedOperationalRepoMock.Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(op => savedOperationalModels.Add(op));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedLamaId.Should().Be("PKB001");
        result.TargetBedId.Should().Be("BED02");
        result.PakaiBedBaruId.Should().NotBeNullOrWhiteSpace();
        result.TglJamTransfer.Should().Be(transferTime);

        // 1. Verify old bed was released
        savedOldBed.Should().NotBeNull();
        savedOldBed!.Periode.Keluar.UserId.Should().Be("nurse_01");
        savedOldBed.Periode.Keluar.Timestamp.Should().Be(transferTime);

        // 2. Verify old bed operational marked as CleaningRequired
        var oldOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED01");
        oldOp.Should().NotBeNull();
        oldOp!.CurrentReadiness.Should().Be(BedReadinessStatusEnum.CleaningRequired);
        oldOp.HasUnresolvedBlocker.Should().BeTrue();
        oldOp.CurrentBlockerState.RestrictionType.Should().Be(BedRestrictionTypeEnum.Cleaning);

        // 3. Verify target bed operational advanced epoch
        var targetOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED02");
        targetOp.Should().NotBeNull();
        targetOp!.OccupancyEpoch.Should().Be(_targetOperationalReady.OccupancyEpoch + 1);

        // 4. Verify new bed allocation created
        savedNewBed.Should().NotBeNull();
        savedNewBed!.Bed.BedId.Should().Be("BED02");
        savedNewBed.Periode.Masuk.UserId.Should().Be("nurse_01");
        savedNewBed.Periode.Masuk.Timestamp.Should().Be(transferTime);
        savedNewBed.Periode.Keluar.Timestamp.Should().Be(new DateTime(3000, 1, 1));
        savedNewBed.Tarif.Should().Be(250000m);
    }

    [Fact]
    public async Task Handle_WhenTargetBedIsOccupied_ThrowsInvalidOperationException()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        // Target bed already has active occupant
        var occupiedBed = CreateActiveOldPakaiBed("PKB999");
        _pakaiBedRepoMock.Setup(x => x.GetActiveOccupant(It.Is<IBedKey>(k => k.BedId == "BED02")))
            .Returns(MayBe.From(occupiedBed));

        var cmd = new PakaiBedTransferCommand("PKB001", "BED02", "nurse_01");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already occupied*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetBedNotReady_ThrowsInvalidOperationException()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        // Target bed is still CleaningRequired
        var dirtyTime = new DateTime(2026, 10, 24, 6, 0, 0, DateTimeKind.Utc);
        var targetDirtyOp = BedOperationalModel.Create("BED02", "BGS01", "KMR02", OccupancyPolicyReff.Default)
            .SetCleaningRequired("Masih kotor", "cleaner_01", dirtyTime, dirtyTime.AddMinutes(1));

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED02")))
            .Returns(MayBe.From(targetDirtyOp));

        var cmd = new PakaiBedTransferCommand("PKB001", "BED02", "nurse_01");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ready*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTargetBedMasterNotActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        _bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED02")))
            .Returns(MayBe.From(_defaultBedTarget with { IsAktif = false }));

        var cmd = new PakaiBedTransferCommand("PKB001", "BED02", "nurse_01");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Handle_WhenTargetBedIsSameAsOldBed_ThrowsInvalidOperationException()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        // Target bed same as old bed "BED01"
        var cmd = new PakaiBedTransferCommand("PKB001", "BED01", "nurse_01");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Target bed cannot be the same as current bed*");
    }

    [Fact]
    public async Task Handle_WhenOldBedNotActive_ThrowsInvalidOperationException()
    {
        // Arrange: old bed is already released
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        oldBed.Release("nurse_prev", new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc));

        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        var cmd = new PakaiBedTransferCommand("PKB001", "BED02", "nurse_01");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Handle_WhenOldBedNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPakaiBed>()))
            .Returns(MayBe<PakaiBedModel>.None);

        var cmd = new PakaiBedTransferCommand("PKB_NONEXISTENT", "BED02", "nurse_01");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }
}

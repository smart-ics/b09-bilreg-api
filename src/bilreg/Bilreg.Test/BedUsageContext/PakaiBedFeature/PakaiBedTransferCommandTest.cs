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
    public async Task Handle_WhenTargetBedOperationalRecordDoesNotExist_CreatesDefaultAndTransfersSuccessfully()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        // Target bed has NO operational record yet
        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED02")))
            .Returns(MayBe<BedOperationalModel>.None);

        var transferTime = new DateTime(2026, 10, 24, 10, 30, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB001",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: transferTime,
            AlasanPindah: "Pindah kamar biasa"
        );

        var savedOperationalModels = new List<BedOperationalModel>();
        _bedOperationalRepoMock.Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(op => savedOperationalModels.Add(op));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedLamaId.Should().Be("PKB001");
        result.TargetBedId.Should().Be("BED02");

        var targetOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED02");
        targetOp.Should().NotBeNull();
        targetOp!.OccupancyEpoch.Should().Be(1);
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

    [Fact]
    public async Task Handle_WhenTargetLayananIdIsInvalidOrBangsalId_FallsBackToExistingLayanan()
    {
        // Arrange
        SetupDefaultMocks();
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        // Setup _layananRepo to return None for "04" (bangsal ID passed as layanan), but Value for default layanan
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "04")))
            .Returns(MayBe<LayananType>.None);
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == _defaultLayanan.LayananId)))
            .Returns(MayBe.From(_defaultLayanan));

        var cmd = new PakaiBedTransferCommand(
            "PKB001",
            "BED02",
            "nurse_01",
            new DateTime(2026, 2, 11, 10, 0, 0),
            "Permintaan pindah",
            null,
            "04");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TargetBedId.Should().Be("BED02");
    }

    private static PakaiBedModel CreateActiveBabyPakaiBed(string pakaiBedId = "PKB_BABY01", string bedId = "BED01", decimal tarif = 0m, string regId = "REG_BABY01")
    {
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff(regId, "PAS_" + regId, "Bayi Ny Budi");
        var layanan = new LayananReff("LYN01", "Bangsal Mawar");
        var bed = new BedReff(bedId, "Bed 01", true);
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
            tarif,
            auditTrail);
    }

    [Fact]
    public async Task Handle_MotherScope_WithRoomingInBaby_CascadesBabyTransferToTargetBedWithZeroTariffAndMarksOldBedDirty()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        var babyBed = CreateActiveBabyPakaiBed("PKB_BABY01", "BED01", 0m);

        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY01")))
            .Returns(MayBe.From(babyBed));

        _pakaiBedRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(new[] { babyBed });
        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyTrsIdsAsync("BED01", It.IsAny<string?>()))
            .ReturnsAsync(new List<string> { "PKB_BABY01" });
        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB001"))
            .ReturnsAsync(new List<string> { "REG_BABY01" });

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG001")));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG_BABY01")));

        var transferTime = new DateTime(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB001",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: transferTime,
            AlasanPindah: "Pindah ibu dan bayi rawat gabung",
            TransferScope: "ibu",
            BabyRegId: "REG_BABY01"
        );

        var savedModels = new List<PakaiBedModel>();
        var savedOperationalModels = new List<BedOperationalModel>();

        _pakaiBedRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedModel>(model => savedModels.Add(model));
        _bedOperationalRepoMock.Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(op => savedOperationalModels.Add(op));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeTrue();
        result.PakaiBedLamaId.Should().Be("PKB001");
        result.TargetBedId.Should().Be("BED02");
        result.BabyPakaiBedBaruId.Should().NotBeNullOrWhiteSpace();

        // 1. Mother old bed released
        oldBed.Periode.Keluar.UserId.Should().Be("nurse_01");
        oldBed.Periode.Keluar.Timestamp.Should().Be(transferTime);

        // 2. Baby old bed released
        babyBed.Periode.Keluar.UserId.Should().Be("nurse_01");
        babyBed.Periode.Keluar.Timestamp.Should().Be(transferTime);

        // 3. Old bed operational marked CleaningRequired
        var oldOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED01");
        oldOp.Should().NotBeNull();
        oldOp!.CurrentReadiness.Should().Be(BedReadinessStatusEnum.CleaningRequired);

        // 4. Target bed operational epoch advanced
        var targetOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED02");
        targetOp.Should().NotBeNull();
        targetOp!.OccupancyEpoch.Should().Be(_targetOperationalReady.OccupancyEpoch + 1);

        // 5. Mother new bed created with normal tariff
        var motherNewBed = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG001" && x.Bed.BedId == "BED02");
        motherNewBed.Should().NotBeNull();
        motherNewBed!.Tarif.Should().Be(250000m);

        // 6. Baby new bed created on BED02 with 0m tariff (maintaining rooming-in)
        var babyNewBed = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG_BABY01" && x.Bed.BedId == "BED02");
        babyNewBed.Should().NotBeNull();
        babyNewBed!.Tarif.Should().Be(0m);
        babyNewBed.KelasTindakanId.Should().Be(motherNewBed.KelasTindakanId);
    }

    [Fact]
    public async Task Handle_BabyScope_InvokedFromMotherBed_TransfersOnlyBabyToTargetBedWithNormalRateAndDoesNotMarkOldBedDirty()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        var babyBed = CreateActiveBabyPakaiBed("PKB_BABY01", "BED01", 0m);

        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY01")))
            .Returns(MayBe.From(babyBed));

        _pakaiBedRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(new[] { babyBed });
        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyTrsIdsAsync("BED01", "REG_BABY01"))
            .ReturnsAsync(new List<string> { "PKB_BABY01" });
        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB001"))
            .ReturnsAsync(new List<string> { "REG_BABY01" });

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG001")));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG_BABY01")));

        var transferTime = new DateTime(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB001",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: transferTime,
            AlasanPindah: "Bayi dipindahkan ke inkubator NICU",
            TransferScope: "bayi",
            BabyRegId: "REG_BABY01"
        );

        var savedModels = new List<PakaiBedModel>();
        var savedOperationalModels = new List<BedOperationalModel>();

        _pakaiBedRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedModel>(model => savedModels.Add(model));
        _bedOperationalRepoMock.Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(op => savedOperationalModels.Add(op));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeFalse();
        result.PakaiBedLamaId.Should().Be("PKB_BABY01");
        result.TargetBedId.Should().Be("BED02");
        result.PakaiBedBaruId.Should().NotBeNullOrWhiteSpace();

        // 1. Mother bed remains ACTIVE and NOT released!
        oldBed.Periode.Keluar.Timestamp.Should().Be(new DateTime(3000, 1, 1));
        savedModels.Should().NotContain(x => x.PakaiBedId == "PKB001");

        // 2. Baby old bed IS released
        babyBed.Periode.Keluar.UserId.Should().Be("nurse_01");
        babyBed.Periode.Keluar.Timestamp.Should().Be(transferTime);

        // 3. Old bed operational BED01 was NOT marked CleaningRequired!
        savedOperationalModels.Should().NotContain(x => x.BedId == "BED01");

        // 4. Target bed operational BED02 advanced epoch
        var targetOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED02");
        targetOp.Should().NotBeNull();
        targetOp!.OccupancyEpoch.Should().Be(_targetOperationalReady.OccupancyEpoch + 1);

        // 5. Baby new bed created on BED02 with NORMAL rate (independent occupancy)
        var babyNewBed = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG_BABY01" && x.Bed.BedId == "BED02");
        babyNewBed.Should().NotBeNull();
        babyNewBed!.Tarif.Should().Be(250000m);
    }

    [Fact]
    public async Task Handle_BabyScope_InvokedDirectlyWithBabyPakaiBedId_TransfersBabyWithoutCleaningOldBed()
    {
        // Arrange
        var babyBed = CreateActiveBabyPakaiBed("PKB_BABY01", "BED01", 0m);

        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY01")))
            .Returns(MayBe.From(babyBed));

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG001")));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG_BABY01")));

        var transferTime = new DateTime(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB_BABY01",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: transferTime,
            AlasanPindah: "Pindah mandiri bayi",
            TransferScope: "bayi"
        );

        var savedModels = new List<PakaiBedModel>();
        var savedOperationalModels = new List<BedOperationalModel>();

        _pakaiBedRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedModel>(model => savedModels.Add(model));
        _bedOperationalRepoMock.Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(op => savedOperationalModels.Add(op));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeFalse();
        result.PakaiBedLamaId.Should().Be("PKB_BABY01");
        result.TargetBedId.Should().Be("BED02");

        // Old bed operational was NOT set to CleaningRequired
        savedOperationalModels.Should().NotContain(x => x.BedId == "BED01");

        // Baby new bed created with normal rate
        var babyNewBed = savedModels.FirstOrDefault(x => x.Bed.BedId == "BED02");
        babyNewBed.Should().NotBeNull();
        babyNewBed!.Tarif.Should().Be(250000m);
    }

    [Fact]
    public void SystemTextJson_Deserialize_WithScopeAndBabyRegId_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            "pakaiBedId": "PKB001",
            "targetBedId": "BED02",
            "userId": "nurse_01",
            "tglJamTransfer": "2026-02-11T11:58:28",
            "alasanPindah": "Pindah kamar VIP",
            "transferScope": "bayi",
            "babyRegId": "REG_BABY01"
        }
        """;

        // Act
        var result = System.Text.Json.JsonSerializer.Deserialize<PakaiBedTransferCommand>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        result.Should().NotBeNull();
        result!.PakaiBedId.Should().Be("PKB001");
        result.TargetBedId.Should().Be("BED02");
        result.UserId.Should().Be("nurse_01");
        result.TransferScope.Should().Be("bayi");
        result.BabyRegId.Should().Be("REG_BABY01");
    }

    [Fact]
    public void SystemTextJson_Deserialize_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            "pakaiBedId": "PKB001",
            "targetBedId": "BED02",
            "userId": "nurse_01",
            "tglJamTransfer": "2026-02-11T11:58:28",
            "alasanPindah": "Pindah kamar VIP"
        }
        """;

        // Act
        var result = System.Text.Json.JsonSerializer.Deserialize<PakaiBedTransferCommand>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        result.Should().NotBeNull();
        result!.PakaiBedId.Should().Be("PKB001");
        result.TargetBedId.Should().Be("BED02");
        result.UserId.Should().Be("nurse_01");
        result.TglJamTransfer.Should().Be(new DateTime(2026, 2, 11, 11, 58, 28));
        result.AlasanPindah.Should().Be("Pindah kamar VIP");
    }

    [Fact]
    public async Task Handle_BabyScope_WhenNoBabyRecordFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");

        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));

        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyTrsIdsAsync("BED01", null))
            .ReturnsAsync(new List<string>());
        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB001"))
            .ReturnsAsync(new List<string>());

        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB001",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: DateTime.UtcNow,
            AlasanPindah: "Pindah mandiri bayi tanpa record",
            TransferScope: "bayi"
        );

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rooming-in baby record not found*");
    }

    [Fact]
    public async Task Handle_MotherScope_WithMultipleRoomingInBabies_CascadesAllBabiesToTargetBed()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        var babyBed1 = CreateActiveBabyPakaiBed("PKB_BABY01", "BED01", 0m, "REG_BABY01");
        var babyBed2 = CreateActiveBabyPakaiBed("PKB_BABY02", "BED01", 0m, "REG_BABY02");

        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY01")))
            .Returns(MayBe.From(babyBed1));
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY02")))
            .Returns(MayBe.From(babyBed2));

        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB001"))
            .ReturnsAsync(new List<string> { "REG_BABY01", "REG_BABY02" });

        _pakaiBedRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(new[] { babyBed1 });
        _pakaiBedRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BABY02")))
            .Returns(new[] { babyBed2 });

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG001")));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG_BABY01")));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY02")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG_BABY02")));

        var transferTime = new DateTime(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB001",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: transferTime,
            AlasanPindah: "Pindah ibu dan bayi kembar",
            TransferScope: "ibu"
        );

        var savedModels = new List<PakaiBedModel>();
        var savedOperationalModels = new List<BedOperationalModel>();

        _pakaiBedRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedModel>(model => savedModels.Add(model));
        _bedOperationalRepoMock.Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(op => savedOperationalModels.Add(op));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeTrue();

        // Old bed BED01 marked CleaningRequired
        var oldOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED01");
        oldOp.Should().NotBeNull();
        oldOp!.CurrentReadiness.Should().Be(BedReadinessStatusEnum.CleaningRequired);

        // Target bed BED02 advanced epoch
        var targetOp = savedOperationalModels.FirstOrDefault(x => x.BedId == "BED02");
        targetOp.Should().NotBeNull();

        // 3 new bed records created on BED02: Mother, Baby 1 (0m), Baby 2 (0m)
        var motherNew = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG001" && x.Bed.BedId == "BED02");
        var baby1New = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG_BABY01" && x.Bed.BedId == "BED02");
        var baby2New = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG_BABY02" && x.Bed.BedId == "BED02");

        motherNew.Should().NotBeNull();
        motherNew!.Tarif.Should().Be(250000m);

        baby1New.Should().NotBeNull();
        baby1New!.Tarif.Should().Be(0m);
        baby1New.KelasTindakanId.Should().Be(motherNew.KelasTindakanId);

        baby2New.Should().NotBeNull();
        baby2New!.Tarif.Should().Be(0m);
        baby2New.KelasTindakanId.Should().Be(motherNew.KelasTindakanId);
    }

    [Fact]
    public async Task Handle_MotherScope_WithRoomingInBaby_BabyInheritsMotherKelasTindakanIdOnTargetBed()
    {
        // Arrange
        var oldBed = CreateActiveOldPakaiBed("PKB001");
        var babyBed = CreateActiveBabyPakaiBed("PKB_BABY01", "BED01", 0m);

        // Mother targets bed with specific Kelas
        var targetKamarCustomKelas = _defaultKamarTarget with
        {
            Kelas = new KelasReff("KLS_VIP", "Kelas VIP")
        };
        var targetKelasCustom = KelasType.Default with { KelasId = "KLS_VIP", KelasName = "Kelas VIP" };

        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KMR02")))
            .Returns(MayBe.From(targetKamarCustomKelas));
        _kelasRepoMock.Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KLS_VIP")))
            .Returns(MayBe.From(targetKelasCustom));

        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(oldBed));
        _pakaiBedRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY01")))
            .Returns(MayBe.From(babyBed));

        _pakaiBedRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(new[] { babyBed });
        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyTrsIdsAsync("BED01", It.IsAny<string?>()))
            .ReturnsAsync(new List<string> { "PKB_BABY01" });
        _pakaiBedRepoMock.Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB001"))
            .ReturnsAsync(new List<string> { "REG_BABY01" });

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG001")));
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(MayBe.From((RegModel)RegModel.Key("REG_BABY01")));

        var transferTime = new DateTime(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedTransferCommand(
            PakaiBedId: "PKB001",
            TargetBedId: "BED02",
            UserId: "nurse_01",
            TglJamTransfer: transferTime,
            AlasanPindah: "Pindah ibu dan bayi rawat gabung ke VIP",
            TransferScope: "ibu",
            BabyRegId: "REG_BABY01"
        );

        var savedModels = new List<PakaiBedModel>();
        _pakaiBedRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedModel>(model => savedModels.Add(model));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var motherNewBed = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG001" && x.Bed.BedId == "BED02");
        var babyNewBed = savedModels.FirstOrDefault(x => x.Reg.RegId == "REG_BABY01" && x.Bed.BedId == "BED02");

        motherNewBed.Should().NotBeNull();
        babyNewBed.Should().NotBeNull();
        motherNewBed!.KelasTindakanId.Should().Be("KLS_VIP");
        babyNewBed!.KelasTindakanId.Should().Be("KLS_VIP");
        babyNewBed.KelasTindakanId.Should().Be(motherNewBed.KelasTindakanId);
    }
}

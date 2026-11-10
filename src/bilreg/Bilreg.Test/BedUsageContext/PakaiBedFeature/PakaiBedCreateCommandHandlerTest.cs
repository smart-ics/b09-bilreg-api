using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedCreateCommandHandlerTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock = new();
    private readonly Mock<IPakaiBedAlokasiRepo> _pakaiBedAlokasiRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<ITipeKamarRepo> _tipeKamarRepoMock = new();
    private readonly Mock<IRoomRateRepo> _roomRateRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<IEmr20AntrianRanapGateway> _emr20GatewayMock = new();

    private readonly PakaiBedCreateCommandHandler _sut;

    private readonly RegModel _defaultReg;
    private readonly BedType _defaultBed;
    private readonly KamarType _defaultKamar;
    private readonly LayananType _defaultLayanan;
    private readonly TipeKamarType _defaultTipeKamar;
    private readonly KelasType _defaultKelas;
    private readonly RoomRateRegulerType _defaultRoomRate;

    public PakaiBedCreateCommandHandlerTest()
    {
        _sut = new PakaiBedCreateCommandHandler(
            _pakaiBedRepoMock.Object,
            _pakaiBedAlokasiRepoMock.Object,
            _regRepoMock.Object,
            _bedRepoMock.Object,
            _layananRepoMock.Object,
            _kamarRepoMock.Object,
            _tipeKamarRepoMock.Object,
            _roomRateRepoMock.Object,
            _kelasRepoMock.Object,
            _waitingListRepoMock.Object,
            _bedOperationalRepoMock.Object,
            _emr20GatewayMock.Object);

        _defaultReg = RegModel.Default;

        _defaultKelas = KelasType.Default with { KelasId = "KLS01", KelasName = "Kelas 1" };
        _defaultKamar = KamarType.Default with
        {
            KamarId = "KMR01",
            KamarName = "Kamar Anggrek 1",
            Kelas = new KelasReff("KLS01", "Kelas 1")
        };
        _defaultBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Kamar = new KamarReff("KMR01", "Kamar Anggrek 1")
        };
        _defaultLayanan = LayananType.Default with { LayananId = "LYN01", LayananName = "Bangsal Anggrek" };
        _defaultTipeKamar = TipeKamarType.Default with { TipeKamarId = "TK01", TipeKamarName = "Standard" };

        _defaultRoomRate = new RoomRateRegulerType(
            "KMR01",
            new KamarReff("KMR01", "Kamar Anggrek 1"),
            new[]
            {
                new RoomRateRegulerTipeType(
                    _defaultTipeKamar.ToReff(),
                    new[] { new RoomRateKomponenType(new KomponenReff("KM01", "Sewa Kamar"), 200000m) })
            });

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(_defaultReg));

        _bedRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedKey>()))
            .Returns(MayBe.From(_defaultBed));

        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe.From(_defaultKamar));

        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(_defaultLayanan));

        _roomRateRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe.From<IRoomRate<IRoomRateDetail>>(_defaultRoomRate));

        _tipeKamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITipeKamarKey>()))
            .Returns(MayBe.From(_defaultTipeKamar));

        _kelasRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe.From(_defaultKelas));

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe<BedOperationalModel>.None);
    }

    [Fact]
    public async Task Handle_ShouldExtractKomponenTarifAndKelasTindakan_AndPassToPakaiBedModel()
    {
        // Arrange
        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: new DateTime(2026, 10, 17, 10, 0, 0));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.Is<PakaiBedModel>(p =>
            p.PakaiBedId == result.PakaiBedId &&
            p.KelasTindakanId == "KLS01" &&
            p.ListKomponen.Count == 1 &&
            p.ListKomponen[0].KomponenId == "KM01" &&
            p.ListKomponen[0].Tarif == 200000m &&
            p.ListKomponen[0].HariKe == 1)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWaitingListId_WhenStatusIsWaiting_ShouldPlaceWaitingListAndSave()
    {
        // Arrange
        var waitingList = WaitingListModel.Default with
        {
            WaitingListId = "WTL001",
            WaitingListStatus = WaitingListStatusEnum.Waiting
        };

        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == "WTL001")))
            .Returns(MayBe.From(waitingList));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: new DateTime(2026, 10, 17, 10, 0, 0),
            WaitingListId: "WTL001");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().NotBeNullOrWhiteSpace();

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.Is<PakaiBedModel>(p =>
            p.PakaiBedId == result.PakaiBedId)), Times.Once);

        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(wl =>
            wl.WaitingListId == "WTL001" &&
            wl.WaitingListStatus == WaitingListStatusEnum.Placed &&
            wl.PlacedBedId == "BED01" &&
            wl.PlacedPakaiBedId == result.PakaiBedId &&
            wl.AuditTrail.Modified.UserId == "USR01")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWaitingListId_WhenStatusIsReserved_ShouldPlaceWaitingListAndSave()
    {
        // Arrange
        var waitingList = WaitingListModel.Default with
        {
            WaitingListId = "WTL002",
            WaitingListStatus = WaitingListStatusEnum.Reserved,
            PlannedBed = new BedReff("BED01", "Bed 01", false)
        };

        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == "WTL002")))
            .Returns(MayBe.From(waitingList));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: new DateTime(2026, 10, 17, 10, 30, 0),
            WaitingListId: "WTL002");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().NotBeNullOrWhiteSpace();

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.Is<PakaiBedModel>(p =>
            p.PakaiBedId == result.PakaiBedId)), Times.Once);

        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(wl =>
            wl.WaitingListId == "WTL002" &&
            wl.WaitingListStatus == WaitingListStatusEnum.Placed &&
            wl.PlacedBedId == "BED01" &&
            wl.PlacedPakaiBedId == result.PakaiBedId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutWaitingListId_ShouldSucceedNormallyWithoutWaitingListRepoCall()
    {
        // Arrange
        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: new DateTime(2026, 10, 17, 11, 0, 0));

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().NotBeNullOrWhiteSpace();

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.Is<PakaiBedModel>(p =>
            p.PakaiBedId == result.PakaiBedId)), Times.Once);

        _waitingListRepoMock.Verify(x => x.LoadEntity(It.IsAny<IWaitingListKey>()), Times.Never);
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithWhitespaceWaitingListId_ShouldSucceedNormallyWithoutWaitingListRepoCall(string whitespaceId)
    {
        // Arrange
        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: new DateTime(2026, 10, 17, 11, 0, 0),
            WaitingListId: whitespaceId);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().NotBeNullOrWhiteSpace();

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.Is<PakaiBedModel>(p =>
            p.PakaiBedId == result.PakaiBedId)), Times.Once);

        _waitingListRepoMock.Verify(x => x.LoadEntity(It.IsAny<IWaitingListKey>()), Times.Never);
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithWaitingListId_WhenWaitingListNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == "NON_EXISTENT")))
            .Returns(MayBe<WaitingListModel>.None);

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: new DateTime(2026, 10, 17, 11, 0, 0),
            WaitingListId: "NON_EXISTENT");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NON_EXISTENT*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
    }

    [Theory]
    [InlineData(WaitingListStatusEnum.Placed)]
    [InlineData(WaitingListStatusEnum.Cancelled)]
    [InlineData(WaitingListStatusEnum.Rerouted)]
    public async Task Handle_WithWaitingListId_WhenWaitingListNotActive_ShouldThrowInvalidOperationException(WaitingListStatusEnum inactiveStatus)
    {
        // Arrange
        var inactiveWl = WaitingListModel.Default with
        {
            WaitingListId = "WTL_INACTIVE",
            WaitingListStatus = inactiveStatus
        };

        _waitingListRepoMock.Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == "WTL_INACTIVE")))
            .Returns(MayBe.From(inactiveWl));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: new DateTime(2026, 10, 17, 11, 0, 0),
            WaitingListId: "WTL_INACTIVE");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not active*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.IsAny<WaitingListModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedReadinessIsCleaningRequired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var bedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default)
            .SetCleaningRequired("Discharge patient", "USR01", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(bedOp));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pembersihan/Housekeeping*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedReadinessIsNotReady_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var bedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default)
            .RecordReadiness(
                BedReadinessStatusEnum.Blocked,
                BedRestrictionTypeEnum.Maintenance,
                "Perbaikan AC",
                "",
                "USR01",
                DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow,
                "BLOCKED-01",
                "REQ-01");

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(bedOp));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak siap pakai*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedHasUnresolvedBlocker_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var blockerState = new BedBlockerStateType(true, BedRestrictionTypeEnum.Maintenance, "Kerusakan fasilitas bed", "TX-01");
        var bedOp = (BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default)
            .VerifyReady("Siap pakai", "", "USR01", "NURSE01", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "READY-01", "REQ-01")) with
        {
            CurrentBlockerState = blockerState
        };

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(bedOp));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: DateTime.UtcNow);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*kendala/blocker aktif*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedOperationalIsReady_ShouldAdvanceOccupancyEpochAndSucceed()
    {
        // Arrange
        var bedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default)
            .VerifyReady("Siap pakai", "", "USR01", "NURSE01", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, "READY-01", "REQ-01");

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(bedOp));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().NotBeNullOrWhiteSpace();

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Once);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" && m.OccupancyEpoch == bedOp.OccupancyEpoch + 1)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPakaiBedCreated_CallsEmr20GatewayAddRegRanapAsync()
    {
        // Arrange
        var cmd = new PakaiBedCreateCommand(
            RegId: "REG01",
            LayananId: "LYN01",
            TipeKamarId: "TK01",
            BedId: "BED01",
            UserId: "USR01",
            TglJamIn: DateTime.UtcNow);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _emr20GatewayMock.Verify(x => x.AddRegRanapAsync("REG01", It.IsAny<CancellationToken>()), Times.Once);
    }
}

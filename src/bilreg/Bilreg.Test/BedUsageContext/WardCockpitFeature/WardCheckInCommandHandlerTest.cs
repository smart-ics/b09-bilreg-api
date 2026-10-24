using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardCheckInCommandHandlerTest
{
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();
    private readonly Mock<IRoomRateRepo> _roomRateRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly WardCheckInCommandHandler _sut;
    private readonly DateTime _defaultTime = new(2026, 10, 24, 11, 0, 0, DateTimeKind.Utc);

    public WardCheckInCommandHandlerTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_defaultTime);

        _sut = new WardCheckInCommandHandler(
            _waitingListRepoMock.Object,
            _bedOperationalRepoMock.Object,
            _bedRepoMock.Object,
            _kamarRepoMock.Object,
            _bangsalRepoMock.Object,
            _roomRateRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);
    }

    private static WaitingListModel CreateSampleWaitingList(
        string id = "WTL-01",
        WaitingListStatusEnum status = WaitingListStatusEnum.Waiting,
        string regId = "REG-01",
        string layananId = "LYN-01")
    {
        return WaitingListModel.Create(
            regId,
            new PasienReff("PS-01", "Budi Santoso", new DateOnly(1980, 1, 1), "L"),
            new KelasReff("KLS-01", "Kelas 1"),
            new BangsalReff("BGS-01", "Bangsal Dahlia"),
            ArrivalSourceTypeEnum.Igd,
            new LayananReff(layananId, "IGD"),
            new DokterReff("DOK-01", "dr. Sutomo, Sp.PD"),
            ClinicalUrgencyEnum.Emergency,
            "Perlu monitor ketat",
            1,
            "ADM-01",
            new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc)) with
        {
            WaitingListId = id,
            WaitingListStatus = status
        };
    }

    private static BedOperationalModel CreateReadyBedOperational(string bedId = "BED-01")
    {
        var model = BedOperationalModel.Create(
            bedId,
            "BGS-01",
            "KMR-01",
            OccupancyPolicyReff.Default);

        return model.VerifyReady(
            "Siap pakai",
            string.Empty,
            "HK-01",
            "NURSE-01",
            DateTime.UtcNow.AddMinutes(-30),
            DateTime.UtcNow.AddMinutes(-30),
            "SF-01",
            "REQ-01");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldDelegateToPakaiBedCreateCommandAndReturnResult()
    {
        // Arrange
        var waitingList = CreateSampleWaitingList();
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.Is<IWaitingListKey>(k => k.WaitingListId == "WTL-01")))
            .Returns(MayBe.From(waitingList));

        var readyBedOp = CreateReadyBedOperational("BED-01");
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(readyBedOp));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBedCreateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBedCreateResult("PKB-20261024-001"));

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-01",
            BedId: "BED-01",
            TipeKamarId: "TK-01",
            UserId: "NURSE-01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().Be("PKB-20261024-001");
        result.WaitingListId.Should().Be("WTL-01");
        result.BedId.Should().Be("BED-01");
        result.RegId.Should().Be("REG-01");
        result.Status.Should().Be("Placed");

        _mediatorMock.Verify(m => m.Send(It.Is<PakaiBedCreateCommand>(c =>
            c.WaitingListId == "WTL-01" &&
            c.BedId == "BED-01" &&
            c.RegId == "REG-01" &&
            c.LayananId == "LYN-01" &&
            c.TipeKamarId == "TK-01" &&
            c.UserId == "NURSE-01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWaitingListNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe<WaitingListModel>.None);

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-MISSING",
            BedId: "BED-01",
            TipeKamarId: "TK-01",
            UserId: "NURSE-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Waiting List 'WTL-MISSING' tidak ditemukan.*");
    }

    [Fact]
    public async Task Handle_WhenWaitingListCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var cancelledWl = CreateSampleWaitingList(status: WaitingListStatusEnum.Cancelled);
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(cancelledWl));

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-01",
            BedId: "BED-01",
            TipeKamarId: "TK-01",
            UserId: "NURSE-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak aktif*Cancelled*");
    }

    [Fact]
    public async Task Handle_WhenWaitingListAlreadyPlaced_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var placedWl = CreateSampleWaitingList(status: WaitingListStatusEnum.Placed);
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(placedWl));

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-01",
            BedId: "BED-01",
            TipeKamarId: "TK-01",
            UserId: "NURSE-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak aktif*Placed*");
    }

    [Fact]
    public async Task Handle_WhenBedOperationalNotReady_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var waitingList = CreateSampleWaitingList();
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        var dirtyBedOp = BedOperationalModel.Create("BED-01", "BGS-01", "KMR-01", OccupancyPolicyReff.Default)
            .RecordReadiness(
                BedReadinessStatusEnum.CleaningRequired,
                BedRestrictionTypeEnum.Cleaning,
                "Checkout",
                "",
                "HK-01",
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow.AddMinutes(-10),
                "SF-01",
                "REQ-01");

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(dirtyBedOp));

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-01",
            BedId: "BED-01",
            TipeKamarId: "TK-01",
            UserId: "NURSE-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Bed 'BED-01' tidak siap pakai*CleaningRequired*");
    }

    [Fact]
    public async Task Handle_WhenBedOperationalHasUnresolvedBlocker_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var waitingList = CreateSampleWaitingList();
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        var blockedBedOp = BedOperationalModel.Create("BED-01", "BGS-01", "KMR-01", OccupancyPolicyReff.Default)
            .RecordReadiness(
                BedReadinessStatusEnum.Blocked,
                BedRestrictionTypeEnum.Maintenance,
                "Engsel tempat tidur patah",
                "",
                "ENG-01",
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow.AddMinutes(-10),
                "SF-02",
                "REQ-02");

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(blockedBedOp));

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-01",
            BedId: "BED-01",
            TipeKamarId: "TK-01",
            UserId: "NURSE-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Bed 'BED-01' tidak siap pakai*");
    }

    [Fact]
    public async Task Handle_WhenTipeKamarIdNotProvided_ShouldResolveFromRoomRate()
    {
        // Arrange
        var waitingList = CreateSampleWaitingList();
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        var readyBedOp = CreateReadyBedOperational("BED-01");
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe.From(readyBedOp));

        var bedType = new BedType(
            "BED-01",
            "Bed 01",
            new KamarReff("KMR-01", "Kamar Anggrek 1"),
            new BangsalReff("BGS-01", "Bangsal Anggrek"),
            true);
        _bedRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedKey>())).Returns(MayBe.From(bedType));

        var kamarType = KamarType.Default with
        {
            KamarId = "KMR-01",
            KamarName = "Kamar Anggrek 1",
            Kelas = new KelasReff("KLS-01", "Kelas 1"),
            Bangsal = new BangsalReff("BGS-01", "Bangsal Anggrek")
        };
        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>())).Returns(MayBe.From(kamarType));

        var roomRate = new RoomRateRegulerType(
            "KMR-01",
            new KamarReff("KMR-01", "Kamar Anggrek 1"),
            new[]
            {
                new RoomRateRegulerTipeType(
                    new TipeKamarReff("TK-RESOLVED", "Standard", true),
                    new[] { new RoomRateKomponenType(new KomponenReff("KM-01", "Sewa"), 350000m) })
            });
        _roomRateRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe.From<IRoomRate<IRoomRateDetail>>(roomRate));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBedCreateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBedCreateResult("PKB-AUTO-01"));

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-01",
            BedId: "BED-01",
            TipeKamarId: null,
            UserId: "NURSE-01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mediatorMock.Verify(m => m.Send(It.Is<PakaiBedCreateCommand>(c =>
            c.TipeKamarId == "TK-RESOLVED"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLayananIdNotProvidedAndWaitingListHasDefault_ShouldResolveFromBangsal()
    {
        // Arrange
        var waitingList = CreateSampleWaitingList(layananId: "-");
        _waitingListRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IWaitingListKey>()))
            .Returns(MayBe.From(waitingList));

        var readyBedOp = CreateReadyBedOperational("BED-01");
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe.From(readyBedOp));

        var bedType = new BedType(
            "BED-01",
            "Bed 01",
            new KamarReff("KMR-01", "Kamar Dahlia 1"),
            new BangsalReff("BGS-01", "Bangsal Dahlia"),
            true);
        _bedRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedKey>())).Returns(MayBe.From(bedType));

        var bangsalType = new BangsalType(
            "BGS-01",
            "Bangsal Dahlia",
            RoomCatType.Default,
            new LayananReff("LYN-BANGSAL-01", "Bangsal Dahlia"));
        _bangsalRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBangsalKey>())).Returns(MayBe.From(bangsalType));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBedCreateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBedCreateResult("PKB-AUTO-02"));

        var cmd = new WardCheckInCommand(
            WaitingListId: "WTL-01",
            BedId: "BED-01",
            TipeKamarId: "TK-01",
            UserId: "NURSE-01");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mediatorMock.Verify(m => m.Send(It.Is<PakaiBedCreateCommand>(c =>
            c.LayananId == "LYN-BANGSAL-01"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

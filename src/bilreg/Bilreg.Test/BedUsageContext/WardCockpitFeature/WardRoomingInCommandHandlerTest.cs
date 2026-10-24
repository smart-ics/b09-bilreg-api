using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.WardCockpitFeature;

public class WardRoomingInCommandHandlerTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly WardRoomingInCommandHandler _sut;
    private readonly DateTime _fixedNowUtc = new(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);

    public WardRoomingInCommandHandlerTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_fixedNowUtc);

        _sut = new WardRoomingInCommandHandler(
            _pakaiBedRepoMock.Object,
            _bedRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task Handle_GivenActiveMotherAndValidBed_ShouldReturnActiveResult()
    {
        var motherPakaiBed = new PakaiBedModel(
            "PKB-MOM",
            new PeriodePakaiBedType(
                new AuditInfoType("ADM-01", _fixedNowUtc.AddDays(-1)),
                new AuditInfoType(string.Empty, new DateTime(3000, 1, 1))),
            new RegReff("REG-MOM", "PAS-01", "Ibu Pasien"),
            LayananType.Default.ToReff(),
            new BedReff("BED-01", "101-A", true),
            TipeKamarType.Default.ToReff(),
            KelasType.Default.ToReff(),
            300000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB-MOM")))
            .Returns(MayBe.From(motherPakaiBed));

        var bed = new BedType(
            "BED-01",
            "101-A",
            new KamarReff("KMR-01", "Kamar 101"),
            new BangsalReff("BGS-01", "Dahlia"),
            true);

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(bed));

        var cmd = new WardRoomingInCommand(
            MotherPakaiBedId: "PKB-MOM",
            BabyRegId: "REG-BABY",
            BedId: "BED-01",
            UserId: "NURSE-01",
            TglJamIn: _fixedNowUtc);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.MotherPakaiBedId.Should().Be("PKB-MOM");
        result.BabyRegId.Should().Be("REG-BABY");
        result.BedId.Should().Be("BED-01");
        result.Status.Should().Be("Active");
        result.TglJamIn.Should().Be(_fixedNowUtc);
    }

    [Fact]
    public async Task Handle_WhenMotherPakaiBedNotFound_ThrowsException()
    {
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBed>()))
            .Returns(MayBe<PakaiBedModel>.None);

        var cmd = new WardRoomingInCommand(
            MotherPakaiBedId: "PKB-NONEXISTENT",
            BabyRegId: "REG-BABY",
            BedId: "BED-01",
            UserId: "NURSE-01");

        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMotherPakaiBedNotActive_ThrowsInvalidOperationException()
    {
        var inactiveMotherPakaiBed = new PakaiBedModel(
            "PKB-MOM",
            new PeriodePakaiBedType(
                new AuditInfoType("ADM-01", _fixedNowUtc.AddDays(-2)),
                new AuditInfoType("NURSE-01", _fixedNowUtc.AddDays(-1))), // already checked out
            new RegReff("REG-MOM", "PAS-01", "Ibu Pasien"),
            LayananType.Default.ToReff(),
            new BedReff("BED-01", "101-A", true),
            TipeKamarType.Default.ToReff(),
            KelasType.Default.ToReff(),
            300000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB-MOM")))
            .Returns(MayBe.From(inactiveMotherPakaiBed));

        var cmd = new WardRoomingInCommand(
            MotherPakaiBedId: "PKB-MOM",
            BabyRegId: "REG-BABY",
            BedId: "BED-01",
            UserId: "NURSE-01");

        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dalam status aktif*");
    }
}

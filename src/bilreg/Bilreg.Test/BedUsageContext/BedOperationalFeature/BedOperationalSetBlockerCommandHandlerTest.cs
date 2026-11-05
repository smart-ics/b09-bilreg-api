using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.BedOperationalFeature;

public class BedOperationalSetBlockerCommandHandlerTest
{
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly BedOperationalSetBlockerCommandHandler _sut;
    private readonly DateTime _fixedNowUtc = new(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);

    public BedOperationalSetBlockerCommandHandlerTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_fixedNowUtc);

        _sut = new BedOperationalSetBlockerCommandHandler(
            _bedOperationalRepoMock.Object,
            _bedRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSettingMaintenanceBlocker_ShouldSetBlockedAndSave()
    {
        var initialModel = BedOperationalModel.Create(
            "BED-01",
            "BGS-01",
            "KMR-01",
            OccupancyPolicyReff.Default);

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(initialModel));

        BedOperationalModel? saved = null;
        _bedOperationalRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(m => saved = m);

        var cmd = new BedOperationalSetBlockerCommand(
            BedId: "BED-01",
            ActorId: "ENG-01",
            Reason: "Perbaikan roda bed",
            RestrictionType: BedRestrictionTypeEnum.Maintenance,
            IsBlocked: true);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.BedId.Should().Be("BED-01");
        result.IsBlocked.Should().BeTrue();
        result.ReadinessStatus.Should().Be("Blocked");

        saved.Should().NotBeNull();
        saved!.HasUnresolvedBlocker.Should().BeTrue();
        saved.CurrentBlockerState.RestrictionType.Should().Be(BedRestrictionTypeEnum.Maintenance);
    }

    [Fact]
    public async Task Handle_WhenResolvingBlocker_ShouldVerifyReadyAndSave()
    {
        var initialModel = BedOperationalModel.Create(
            "BED-01",
            "BGS-01",
            "KMR-01",
            OccupancyPolicyReff.Default);

        var blockedModel = initialModel.RecordReadiness(
            BedReadinessStatusEnum.Blocked,
            BedRestrictionTypeEnum.Maintenance,
            "Roda rusak",
            "REF-01",
            "ENG-01",
            _fixedNowUtc.AddHours(-2),
            _fixedNowUtc.AddHours(-2),
            "SF-01",
            "REQ-01");

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(blockedModel));

        BedOperationalModel? saved = null;
        _bedOperationalRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(m => saved = m);

        var cmd = new BedOperationalSetBlockerCommand(
            BedId: "BED-01",
            ActorId: "ENG-01",
            Reason: "Perbaikan selesai",
            RestrictionType: BedRestrictionTypeEnum.Maintenance,
            IsBlocked: false);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.BedId.Should().Be("BED-01");
        result.IsBlocked.Should().BeFalse();
        result.ReadinessStatus.Should().Be("Ready");

        saved.Should().NotBeNull();
        saved!.HasUnresolvedBlocker.Should().BeFalse();
    }
}

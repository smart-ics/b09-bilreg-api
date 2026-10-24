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

public class BedOperationalVerifyReadyCommandHandlerTest
{
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly BedOperationalVerifyReadyCommandHandler _sut;
    private readonly DateTime _fixedNowUtc = new(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc);

    public BedOperationalVerifyReadyCommandHandlerTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_fixedNowUtc);

        _sut = new BedOperationalVerifyReadyCommandHandler(
            _bedOperationalRepoMock.Object,
            _bedRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBedOperationalExists_ShouldVerifyReadyAndSave()
    {
        // Arrange
        var initialModel = BedOperationalModel.Create(
            "BED-01",
            "BGS-01",
            "KMR-01",
            OccupancyPolicyReff.Default);

        var dirtyModel = initialModel.RecordReadiness(
            BedReadinessStatusEnum.CleaningRequired,
            BedRestrictionTypeEnum.Cleaning,
            "Pasien checkout",
            "REF-01",
            "USER-HK",
            _fixedNowUtc.AddHours(-1),
            _fixedNowUtc.AddHours(-1),
            "SF-01",
            "REQ-01");

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(dirtyModel));

        var cmd = new BedOperationalVerifyReadyCommand(
            BedId: "BED-01",
            VerifiedByActorId: "NURSE-01",
            ResponsibleActorId: "HK-02",
            Reason: "Pembersihan kamar dan disinfeksi selesai",
            EvidenceReference: "CHK-20261024-001");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BedId.Should().Be("BED-01");
        result.ReadinessStatus.Should().Be("Ready");
        result.LatestTransactionId.Should().NotBeNullOrWhiteSpace();

        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED-01" &&
            m.CurrentReadiness == BedReadinessStatusEnum.Ready &&
            m.IsReadinessVerified &&
            !m.HasUnresolvedBlocker &&
            m.LatestReadinessTransactionId == result.LatestTransactionId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedOperationalDoesNotExist_ShouldCreateFromBedRepoAndVerifyReady()
    {
        // Arrange
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe<BedOperationalModel>.None);

        var bedType = new BedType(
            "BED-02",
            "Bed 02",
            new KamarReff("KMR-02", "Kamar 02"),
            new BangsalReff("BGS-02", "Bangsal Mawar"),
            true);

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED-02")))
            .Returns(MayBe.From(bedType));

        var cmd = new BedOperationalVerifyReadyCommand(
            BedId: "BED-02",
            VerifiedByActorId: "NURSE-02");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BedId.Should().Be("BED-02");
        result.ReadinessStatus.Should().Be("Ready");

        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED-02" &&
            m.BangsalId == "BGS-02" &&
            m.KamarId == "KMR-02" &&
            m.CurrentReadiness == BedReadinessStatusEnum.Ready &&
            m.IsReadinessVerified)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedNotFoundAnywhere_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe<BedOperationalModel>.None);

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedKey>()))
            .Returns(MayBe<BedType>.None);

        var cmd = new BedOperationalVerifyReadyCommand(
            BedId: "BED-NONEXISTENT",
            VerifiedByActorId: "NURSE-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Bed 'BED-NONEXISTENT' tidak ditemukan.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenBedIdEmpty_ShouldThrowArgumentException(string? invalidBedId)
    {
        // Arrange
        var cmd = new BedOperationalVerifyReadyCommand(
            BedId: invalidBedId!,
            VerifiedByActorId: "NURSE-01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenVerifiedByActorIdEmpty_ShouldThrowArgumentException(string? invalidActorId)
    {
        // Arrange
        var cmd = new BedOperationalVerifyReadyCommand(
            BedId: "BED-01",
            VerifiedByActorId: invalidActorId!);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithCustomOccurredAt_ShouldNormalizeAndSave()
    {
        // Arrange
        var initialModel = BedOperationalModel.Create(
            "BED-03",
            "BGS-01",
            "KMR-01",
            OccupancyPolicyReff.Default);

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-03")))
            .Returns(MayBe.From(initialModel));

        var specificTime = new DateTime(2026, 10, 24, 8, 30, 0, DateTimeKind.Utc);
        var cmd = new BedOperationalVerifyReadyCommand(
            BedId: "BED-03",
            VerifiedByActorId: "NURSE-01",
            OccurredAt: specificTime,
            Reason: "Selesai desinfeksi",
            RequestId: "REQ-CUSTOM-123");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.OccurredAt.Should().Be(specificTime);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.ListReadinessTransaction.Any(t => t.RequestId == "REQ-CUSTOM-123" && t.Reason == "Selesai desinfeksi"))),
            Times.Once);
    }
}

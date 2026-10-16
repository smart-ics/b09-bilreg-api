using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedReleaseCommandHandlerTest
{
    private readonly Mock<IPakaiBedAlokasiRepo> _pakaiBedAlokasiRepoMock = new();
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();

    private static readonly DateTime ProposedAt = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime AssignedAt = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReleasedAt = new(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);

    private PakaiBedReleaseCommandHandler CreateSut() =>
        new(_pakaiBedAlokasiRepoMock.Object, _bedOperationalRepoMock.Object);

    private static PakaiBedAlokasiModel CreateActiveAlokasi(
        string pakaiBedId = "PKB-001",
        string bedId = "BED-01",
        string physicalKelasId = "KL1",
        string billingKelasId = "VIP",
        string tipeKamarId = "TK1",
        decimal tarif = 750000m,
        string motherPakaiBedId = "",
        bool isTitipKelas = true,
        bool isNaikKelas = false)
    {
        var proposed = PakaiBedAlokasiModel.Propose(
            "REG-001",
            "PAS-001",
            "BGS-01",
            "KMR-01",
            bedId,
            "WTL-01",
            "REQ-01",
            PakaiBedPurposeEnum.Clinical,
            OccupantRoleEnum.Primary,
            "PETUGAS-ADM",
            ProposedAt,
            ProposedAt.AddMinutes(1),
            "Clinical allocation proposal",
            physicalKelasId,
            billingKelasId,
            tipeKamarId,
            tarif,
            motherPakaiBedId,
            isTitipKelas,
            isNaikKelas);

        return proposed.Assign(
            "PETUGAS-NURSE",
            AssignedAt,
            AssignedAt.AddMinutes(1),
            "Bed assignment",
            "EVD-01") with { PakaiBedId = pakaiBedId };
    }

    private static BedOperationalModel CreateReadyBedOperational(string bedId = "BED-01")
    {
        var initial = BedOperationalModel.Create(bedId, "BGS-01", "KMR-01", new OccupancyPolicyReff("POL-1", "Standard"));
        return initial.VerifyReady(
            "Ready for use",
            "EVD-INSPECT",
            "PETUGAS-HK",
            "PETUGAS-VERIF",
            AssignedAt.AddHours(-1),
            AssignedAt.AddHours(-1).AddMinutes(1),
            "SRC-01",
            "REQ-01");
    }

    [Fact]
    public async Task Handle_WhenValidActiveAllocation_ReleasesAllocationAndSetsBedCleaningRequiredAndSavesBoth()
    {
        // Arrange
        var alokasi = CreateActiveAlokasi();
        var bedOp = CreateReadyBedOperational();

        _pakaiBedAlokasiRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == "PKB-001")))
            .Returns(MayBe.From(alokasi));

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(bedOp));

        PakaiBedAlokasiModel? savedAlokasi = null;
        BedOperationalModel? savedBedOp = null;

        _pakaiBedAlokasiRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()))
            .Callback<PakaiBedAlokasiModel>(m => savedAlokasi = m);

        _bedOperationalRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(m => savedBedOp = m);

        var cmd = new PakaiBedReleaseCommand(
            PakaiBedId: "PKB-001",
            TglJamOut: ReleasedAt,
            Reason: "Pasien KRS sembuh",
            PetugasOut: "PETUGAS-OUT-01");

        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _pakaiBedAlokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Once);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Once);

        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.PakaiBedId.Should().Be("PKB-001");
        savedAlokasi.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Released);
        savedAlokasi.ListTransition.Should().Contain(t =>
            t.PakaiBedTransition == PakaiBedTransisiEnum.Released &&
            t.OccurredAt == ReleasedAt &&
            t.Reason == "Pasien KRS sembuh" &&
            t.ActorId == "PETUGAS-OUT-01");

        savedBedOp.Should().NotBeNull();
        savedBedOp!.BedId.Should().Be("BED-01");
        savedBedOp.CurrentReadiness.Should().Be(BedReadinessStatusEnum.CleaningRequired);
        savedBedOp.CurrentBlockerState.IsBlocked.Should().BeTrue();
        savedBedOp.CurrentBlockerState.RestrictionType.Should().Be(BedRestrictionTypeEnum.Cleaning);
        savedBedOp.CurrentBlockerState.Reason.Should().Be("Pasien KRS sembuh");
        savedBedOp.ListReadinessTransaction.Should().Contain(t =>
            t.NewStatus == BedReadinessStatusEnum.CleaningRequired &&
            t.RestrictionType == BedRestrictionTypeEnum.Cleaning &&
            t.ResponsibleActorId == "PETUGAS-OUT-01" &&
            t.OccurredAt == ReleasedAt &&
            t.SourceFactId == "PKB-001");
    }

    [Fact]
    public async Task Handle_PreservesDualClassAndRoomingInAttributes()
    {
        // Arrange
        var alokasi = CreateActiveAlokasi(
            physicalKelasId: "KL2",
            billingKelasId: "VIP",
            tipeKamarId: "TK2",
            tarif: 1200000m,
            motherPakaiBedId: "PKB-MOM-01",
            isTitipKelas: true,
            isNaikKelas: false);
        var bedOp = CreateReadyBedOperational();

        _pakaiBedAlokasiRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(MayBe.From(alokasi));
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe.From(bedOp));

        PakaiBedAlokasiModel? savedAlokasi = null;
        _pakaiBedAlokasiRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()))
            .Callback<PakaiBedAlokasiModel>(m => savedAlokasi = m);

        var cmd = new PakaiBedReleaseCommand("PKB-001", ReleasedAt, "Discharge", "PETUGAS-01");
        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.PhysicalKelasId.Should().Be("KL2");
        savedAlokasi.BillingKelasId.Should().Be("VIP");
        savedAlokasi.TipeKamarId.Should().Be("TK2");
        savedAlokasi.Tarif.Should().Be(1200000m);
        savedAlokasi.MotherPakaiBedId.Should().Be("PKB-MOM-01");
        savedAlokasi.IsTitipKelas.Should().BeTrue();
        savedAlokasi.IsNaikKelas.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAlokasiNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _pakaiBedAlokasiRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(MayBe<PakaiBedAlokasiModel>.None);

        var cmd = new PakaiBedReleaseCommand("PKB-MISSING", ReleasedAt, "Checkout", "PETUGAS-01");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
        _pakaiBedAlokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedOperationalNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var alokasi = CreateActiveAlokasi();
        _pakaiBedAlokasiRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(MayBe.From(alokasi));
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe<BedOperationalModel>.None);

        var cmd = new PakaiBedReleaseCommand("PKB-001", ReleasedAt, "Checkout", "PETUGAS-01");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
        _pakaiBedAlokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlokasiAlreadyReleased_ThrowsInvalidOperationException()
    {
        // Arrange
        var releasedAlokasi = CreateActiveAlokasi().Release(ReleasedAt.AddDays(-1), "First release", "PETUGAS-01");
        _pakaiBedAlokasiRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(MayBe.From(releasedAlokasi));

        var cmd = new PakaiBedReleaseCommand("PKB-001", ReleasedAt, "Second release", "PETUGAS-02");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*harus Active untuk dilepas*");
    }

    [Fact]
    public async Task Handle_WhenOccurredAtBeforeStartedAt_ThrowsArgumentException()
    {
        // Arrange
        var alokasi = CreateActiveAlokasi();
        _pakaiBedAlokasiRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(MayBe.From(alokasi));

        var invalidTime = AssignedAt.AddHours(-1);
        var cmd = new PakaiBedReleaseCommand("PKB-001", invalidTime, "Checkout", "PETUGAS-01");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak boleh lebih awal dari waktu mulai*");
    }

    [Fact]
    public async Task Handle_WhenOccurredAtNonUtc_ThrowsArgumentException()
    {
        // Arrange
        var alokasi = CreateActiveAlokasi();
        _pakaiBedAlokasiRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()))
            .Returns(MayBe.From(alokasi));

        var localTime = DateTime.SpecifyKind(ReleasedAt, DateTimeKind.Local);
        var cmd = new PakaiBedReleaseCommand("PKB-001", localTime, "Checkout", "PETUGAS-01");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*DateTimeKind.Utc*");
    }

    [Theory]
    [InlineData("", "Discharge", "PETUGAS-01")]
    [InlineData("   ", "Discharge", "PETUGAS-01")]
    [InlineData("PKB-001", "", "PETUGAS-01")]
    [InlineData("PKB-001", "   ", "PETUGAS-01")]
    [InlineData("PKB-001", "Discharge", "")]
    [InlineData("PKB-001", "Discharge", "   ")]
    public async Task Handle_WithNullOrWhitespaceArguments_Throws(
        string pakaiBedId,
        string reason,
        string petugasOut)
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand(pakaiBedId, ReleasedAt, reason, petugasOut);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

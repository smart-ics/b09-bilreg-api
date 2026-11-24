using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardCockpitFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedVoidCommandTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<IPakaiBedBillingLockChecker> _billingLockCheckerMock = new();
    private readonly Mock<IPakaiBedDal> _pakaiBedDalMock = new();
    private readonly Mock<ITindakanRepo> _tindakanRepoMock = new();
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<IWardCockpitEventPublisher> _eventPublisherMock = new();
    private readonly Mock<IEmr20AntrianRanapGateway> _emr20GatewayMock = new();
    private readonly PakaiBedDeleteCommandHandler _sut;

    public PakaiBedVoidCommandTest()
    {
        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe<BedOperationalModel>.None);

        _pakaiBedDalMock
            .Setup(x => x.VoidBedAndRoomChargeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<string>());

        _sut = new PakaiBedDeleteCommandHandler(
            _pakaiBedRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _billingLockCheckerMock.Object,
            _pakaiBedDalMock.Object,
            _tindakanRepoMock.Object,
            _waitingListRepoMock.Object,
            bedOperationalRepo: _bedOperationalRepoMock.Object,
            eventPublisher: _eventPublisherMock.Object,
            emr20Gateway: _emr20GatewayMock.Object);
    }

    private static PakaiBedModel CreateTestBed(string pakaiBedId = "PKB001", string regId = "REG001", string layananId = "LAY01")
    {
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 15, 10, 0, 0));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff(regId, "PAS001", "Pasien A");
        var layanan = new LayananReff(layananId, "Ranap Mawar");
        var bed = new BedReff("BED01", "Bed 01", true);
        var tipeKamar = new TipeKamarReff("TK01", "Standar", true);
        var kelas = new KelasReff("KL01", "Kelas 1");
        var auditTrail = new AuditTrailType(masuk, AuditInfoType.Default, AuditInfoType.Default);

        return new PakaiBedModel(
            pakaiBedId,
            periode,
            reg,
            layanan,
            bed,
            tipeKamar,
            kelas,
            300000m,
            auditTrail);
    }

    [Fact]
    public async Task Handle_WhenBedFound_ExecutesAtomicVoidOnBedAndRoomCharge_AndPreservesClinicalTindakan()
    {
        // Arrange
        var bed = CreateTestBed("PKB001", "REG001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG001"))
            .Returns(false);

        var now = new DateTime(2026, 11, 3, 10, 30, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        // Pasien memiliki tindakan aktif yang sudah di-input
        var activeTindakan = new TindakanJualView(
            "TDK001",
            new DateTime(2026, 11, 3, 9, 0, 0),
            "ORD001",
            new RegReff("REG001", "PAS001", "Pasien A"),
            new LayananReff("LAY01", "Ranap Mawar"),
            "TRF01",
            "Pemberian Infus RL",
            1,
            75000m,
            "TINDAKAN",
            "PKB001");

        _tindakanRepoMock
            .Setup(x => x.ListDataTdkJual(It.Is<IRegKey>(r => r.RegId == "REG001")))
            .Returns(new[] { activeTindakan });

        var cmd = new PakaiBedVoidCommand("PKB001", "nurse_john", "Batal check-in: Salah nomor bed", "Mozilla/5.0", "192.168.1.100");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert 1: PakaiBed domain model is voided and saved
        bed.AuditTrail.Voided.UserId.Should().Be("nurse_john");
        bed.AuditTrail.Voided.Timestamp.Should().Be(now);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(bed), Times.Once);

        // Assert 2: Atomic void on bed and room charge executed via DAL
        _pakaiBedDalMock.Verify(x => x.VoidBedAndRoomChargeAsync(
            "PKB001",
            "nurse_john",
            "Batal check-in: Salah nomor bed",
            now), Times.Once);

        // Assert 3: ta_trs_tindakan is STRICTLY PRESERVED (never deleted, voided, or modified)
        _tindakanRepoMock.Verify(x => x.Delete(It.IsAny<ITindakanKey>()), Times.Never);
        _tindakanRepoMock.Verify(x => x.SaveChanges(It.IsAny<TindakanModel>()), Times.Never);

        // Assert 4: Audit log is recorded
        _auditRepoMock.Verify(x => x.SaveChanges(It.Is<AuditLog>(a =>
            a.ActionType == "VOID" &&
            a.EntityName == nameof(PakaiBedModel) &&
            a.EntityId == "PKB001" &&
            a.Reason == "Batal check-in: Salah nomor bed" &&
            a.CorrelationId == "REG001"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBillingIsLocked_ThrowsBillingLockedException_AndDoesNotModifyBedOrRoomCharge()
    {
        // Arrange
        var bed = CreateTestBed("PKB001", "REG001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG001"))
            .Returns(true);

        var cmd = new PakaiBedVoidCommand("PKB001", "nurse_john", "Batal check-in", "Mozilla/5.0", "127.0.0.1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<BillingLockedException>()
            .WithMessage("*REG001*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
        _pakaiBedDalMock.Verify(x => x.VoidBedAndRoomChargeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBedNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBed>()))
            .Returns(MayBe<PakaiBedModel>.None);

        var cmd = new PakaiBedVoidCommand("NONEXISTENT", "nurse_john", "Batal", "Agent", "127.0.0.1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NONEXISTENT*");

        _pakaiBedDalMock.Verify(x => x.VoidBedAndRoomChargeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [Theory]
    [InlineData("", "usr1", "reason")]
    [InlineData("PKB001", "", "reason")]
    [InlineData("PKB001", "usr1", "")]
    public async Task Handle_WhenArgumentsInvalid_ThrowsArgumentException(string pakaiBedId, string userId, string voidReason)
    {
        // Arrange
        var cmd = new PakaiBedVoidCommand(pakaiBedId, userId, voidReason, "Agent", "127.0.0.1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenVoided_RevertsMatchedWaitingListToWaiting()
    {
        // Arrange
        var bed = CreateTestBed("PKB001", "REG001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG001"))
            .Returns(false);

        var now = new DateTime(2026, 11, 3, 11, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var wl = WaitingListModel.Create(
            "REG001",
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new KelasReff("KL01", "Kelas 1"),
            new BangsalReff("BSL01", "Bangsal Mawar"),
            0,
            "admin",
            now.AddHours(-2))
            .Place("BED01", "PKB001", "admin", now.AddHours(-1));

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG001"))
            .Returns(new[] { wl });

        var cmd = new PakaiBedVoidCommand("PKB001", "nurse_john", "Batal check-in", "Agent", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(m =>
            m.WaitingListStatus == WaitingListStatusEnum.Waiting &&
            m.PlacedBedId == string.Empty &&
            m.PlacedPakaiBedId == string.Empty
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVoided_SetsBedOperationalToCleaningRequired_WithBatalCheckInReason()
    {
        // Arrange
        var bed = CreateTestBed("PKB001", "REG001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG001"))
            .Returns(false);

        var now = new DateTime(2026, 11, 3, 11, 30, 0, DateTimeKind.Utc);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var cmd = new PakaiBedVoidCommand("PKB001", "nurse_john", "Batal check-in: Salah pasien", "Mozilla/5.0", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert: bed operational is set to CleaningRequired with reason "Pembersihan Pasca Batal Check-In: <alasan>"
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Pembersihan Pasca Batal Check-In: Batal check-in: Salah pasien" &&
                t.ResponsibleActorId == "nurse_john" &&
                t.SourceFactId.StartsWith("FACT-VOID-PKB001"))
        )), Times.Once);

        // Assert: SignalR "Cleaning" and waiting list update are emitted
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("LAY01", "BED01", "Cleaning", null, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("LAY01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVoidedAndBedOperationalExists_SetsBedOperationalToCleaningRequired()
    {
        // Arrange
        var bed = CreateTestBed("PKB002", "REG002");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB002")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG002"))
            .Returns(false);

        var now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var existingBedOp = BedOperationalModel.Create("BED01", "LAY01", "DEFAULT", OccupancyPolicyReff.Default)
            .VerifyReady("Initial", "", "admin", "admin", now.AddHours(-1), now.AddHours(-1), "FACT-INIT", "REQ-INIT");

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(existingBedOp));

        var cmd = new PakaiBedVoidCommand("PKB002", "nurse_jane", "Batal check-in", "Mozilla/5.0", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Pembersihan Pasca Batal Check-In: Batal check-in" &&
                t.ResponsibleActorId == "nurse_jane")
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedHasFutureTransactionInHistory_AdjustsOccurredAtToPreventSuperseding_AndSetsCleaningRequired()
    {
        // Arrange
        var bed = CreateTestBed("PKB003", "REG003");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB003")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG003"))
            .Returns(false);

        var now = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var futureOccurredAt = now.AddMinutes(30);
        var existingBedOp = BedOperationalModel.Create("BED01", "LAY01", "DEFAULT", OccupancyPolicyReff.Default)
            .VerifyReady("Future verification", "", "admin", "admin", futureOccurredAt, futureOccurredAt, "FACT-FUTURE", "REQ-FUTURE");

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(existingBedOp));

        var cmd = new PakaiBedVoidCommand("PKB003", "nurse_future", "Batal check-in masa depan", "Mozilla/5.0", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Pembersihan Pasca Batal Check-In: Batal check-in masa depan" &&
                t.ResponsibleActorId == "nurse_future" &&
                t.OccurredAt >= futureOccurredAt &&
                t.NewStatus == BedReadinessStatusEnum.CleaningRequired)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVoidedRoomingInBaby_RevertsWaitingListAndPublishesSignalRToBothWards()
    {
        // Arrange: Baby's bed transaction in Bangsal Mawar (LAY01), but baby's waiting list was from Bangsal Perinatologi (BSL11)
        var bed = CreateTestBed("PKB_BABY01", "REG_BABY01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY01")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG_BABY01"))
            .Returns(false);

        var now = new DateTime(2026, 11, 4, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var babyWl = WaitingListModel.Create(
            "REG_BABY01",
            new PasienReff("MR_BABY01", "Bayi Ny. A", new DateOnly(2026, 11, 1), "L"),
            new KelasReff("KL01", "Kelas 1"),
            new BangsalReff("BSL11", "Bangsal Perinatologi"),
            0,
            "admin",
            now.AddHours(-3))
            .Place("BED01", "PKB_BABY01", "admin", now.AddHours(-1));

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG_BABY01"))
            .Returns(new[] { babyWl });

        var cmd = new PakaiBedVoidCommand("PKB_BABY01", "nurse_eva", "Batal rawat gabung", "Mozilla/5.0", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert 1: Baby waiting list reverted to Waiting
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(m =>
            m.WaitingListId == babyWl.WaitingListId &&
            m.WaitingListStatus == WaitingListStatusEnum.Waiting &&
            m.PlacedBedId == string.Empty &&
            m.PlacedPakaiBedId == string.Empty
        )), Times.Once);

        // Assert 2: SignalR sent to mother's ward (LAY01) AND baby's waiting list ward (BSL11)
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("LAY01", "BED01", "Cleaning", null, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("BSL11", "BED01", "Cleaning", null, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("LAY01", It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BSL11", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVoidedMotherWithRoomingInBaby_DalReturnsBabyWard_PublishesSignalRToBothWards()
    {
        // Arrange: Mother's bed transaction in Bangsal Mawar (LAY01)
        var motherBed = CreateTestBed("PKB_MOTHER01", "REG_MOTHER01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_MOTHER01")))
            .Returns(MayBe.From(motherBed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG_MOTHER01"))
            .Returns(false);

        var now = new DateTime(2026, 11, 4, 11, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        // DAL returns BSL11 because it cascade-reverted a rooming-in baby waiting list in Perinatologi
        _pakaiBedDalMock
            .Setup(x => x.VoidBedAndRoomChargeAsync("PKB_MOTHER01", "nurse_eva", "Batal check-in ibu", now))
            .ReturnsAsync(new List<string> { "BSL11" });

        var cmd = new PakaiBedVoidCommand("PKB_MOTHER01", "nurse_eva", "Batal check-in ibu", "Mozilla/5.0", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert: bed operational is set to CleaningRequired for the shared physical bed
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Pembersihan Pasca Batal Check-In: Batal check-in ibu" &&
                t.ResponsibleActorId == "nurse_eva")
        )), Times.Once);

        // Assert: SignalR sent to mother's ward (LAY01) and baby's ward (BSL11)
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("LAY01", "BED01", "Cleaning", null, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("BSL11", "BED01", "Cleaning", null, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("LAY01", It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BSL11", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedRepoResolvesPhysicalBangsal_UsesPhysicalBangsalInsteadOfLayananIdForSignalR()
    {
        // Arrange: Bed in Bangsal St. Anna ("04"), but Layanan is "3RI05"
        var bed = CreateTestBed("PKB002", "REG002", layananId: "3RI05");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB002")))
            .Returns(MayBe.From(bed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG002"))
            .Returns(false);

        var now = new DateTime(2026, 11, 4, 12, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var bedRepoMock = new Mock<IBedRepo>();
        var physicalBed = new BedType(
            "BED01",
            "Bed 01",
            new KamarReff("KMR01", "Kamar 101"),
            new BangsalReff("04", "St. Anna"),
            true);
        bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(physicalBed));

        var sut = new PakaiBedDeleteCommandHandler(
            _pakaiBedRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _billingLockCheckerMock.Object,
            _pakaiBedDalMock.Object,
            _tindakanRepoMock.Object,
            _waitingListRepoMock.Object,
            bedRepo: bedRepoMock.Object,
            bedOperationalRepo: _bedOperationalRepoMock.Object,
            eventPublisher: _eventPublisherMock.Object,
            emr20Gateway: _emr20GatewayMock.Object);

        var cmd = new PakaiBedVoidCommand("PKB002", "nurse_anna", "Batal check-in", "Mozilla/5.0", "127.0.0.1");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert: SignalR sent to physical ward "04", NEVER to "3RI05"
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("04", "BED01", "Cleaning", null, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("04", It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("3RI05", It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("3RI05", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVoidedMotherWithRoomingInBaby_AndBabyHasNoWaitingList_CreatesFallbackWaitingListForBabyWithRawatGabungNote()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_MOTHER01", "REG_MOTHER01", "LAY01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_MOTHER01")))
            .Returns(MayBe.From(motherBed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG_MOTHER01"))
            .Returns(false);

        var now = new DateTime(2026, 11, 5, 10, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        // Mother has placed waiting list
        var motherWl = WaitingListModel.Create(
            "REG_MOTHER01",
            new PasienReff("MR_MOTHER01", "Ny. Mother", new DateOnly(1995, 5, 15), "P"),
            new KelasReff("KL01", "Kelas 1"),
            new BangsalReff("LAY01", "Ranap Mawar"),
            0,
            "admin",
            now.AddHours(-3))
            .Place("BED01", "PKB_MOTHER01", "admin", now.AddHours(-1));

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG_MOTHER01"))
            .Returns(new[] { motherWl });

        // DAL returns rooming-in baby registration
        _pakaiBedDalMock
            .Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB_MOTHER01"))
            .ReturnsAsync(new List<string> { "REG_BABY01" });

        // Baby has NO active waiting list in repository (legacy check-in without waiting list)
        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG_BABY01"))
            .Returns(Enumerable.Empty<WaitingListModel>());

        // RegRepo provides baby registration details
        var regRepoMock = new Mock<IRegRepo>();
        var babyReg = new RegModel(
            "REG_BABY01",
            new DateOnly(2026, 11, 4),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("MR_BABY01", "By. Ny. Mother", new DateOnly(2026, 11, 4), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            new KelasReff("KL01", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff("DOK01", "dr. Anak"),
            new LayananReff("LAY01", "Ranap Mawar"),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            [],
            bangsal: new BangsalReff("LAY01", "Ranap Mawar"),
            jenisInap: new JenisInapType("3", "Perinatal"),
            regIbu: "REG_MOTHER01");

        regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY01")))
            .Returns(MayBe.From(babyReg));

        var sut = new PakaiBedDeleteCommandHandler(
            _pakaiBedRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _billingLockCheckerMock.Object,
            _pakaiBedDalMock.Object,
            _tindakanRepoMock.Object,
            _waitingListRepoMock.Object,
            regRepo: regRepoMock.Object,
            bedOperationalRepo: _bedOperationalRepoMock.Object,
            eventPublisher: _eventPublisherMock.Object,
            emr20Gateway: _emr20GatewayMock.Object);

        var cmd = new PakaiBedVoidCommand("PKB_MOTHER01", "nurse_v", "Batal check-in ibu & bayi", "Mozilla/5.0", "127.0.0.1");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert 1: Mother's waiting list reverted to Waiting
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(w =>
            w.RegId == "REG_MOTHER01" &&
            w.WaitingListStatus == WaitingListStatusEnum.Waiting
        )), Times.Once);

        // Assert 2: Fallback waiting list created for baby with note "Batal Check-in Rawat Gabung"
        _waitingListRepoMock.Verify(x => x.Save(It.Is<WaitingListModel>(w =>
            w.RegId == "REG_BABY01" &&
            w.WaitingListStatus == WaitingListStatusEnum.Waiting &&
            w.SpecialRequirements == "Batal Check-in Rawat Gabung" &&
            w.Bangsal.BangsalId == "LAY01" &&
            w.Pasien.PasienId == "MR_BABY01"
        )), Times.Once);

        // Assert 3: SignalR event dispatched for ward
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("LAY01", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WhenVoidedMotherWithRoomingInBaby_AndBabyHasPlacedWaitingList_RevertsBabyWaitingListToWaiting()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_MOTHER02", "REG_MOTHER02", "LAY01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_MOTHER02")))
            .Returns(MayBe.From(motherBed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG_MOTHER02"))
            .Returns(false);

        var now = new DateTime(2026, 11, 5, 11, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        _pakaiBedDalMock
            .Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB_MOTHER02"))
            .ReturnsAsync(new List<string> { "REG_BABY02" });

        var babyWl = WaitingListModel.Create(
            "REG_BABY02",
            new PasienReff("MR_BABY02", "By. Ny. Mother 2", new DateOnly(2026, 11, 4), "L"),
            new KelasReff("KL01", "Kelas 1"),
            new BangsalReff("BSL11", "Bangsal Perinatologi"),
            0,
            "admin",
            now.AddHours(-2))
            .Place("BED01", "PKB_BABY02", "admin", now.AddHours(-1));

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG_BABY02"))
            .Returns(new[] { babyWl });

        var cmd = new PakaiBedVoidCommand("PKB_MOTHER02", "nurse_v", "Batal check-in ibu", "Mozilla/5.0", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert: Baby's waiting list is reverted to Waiting
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(w =>
            w.RegId == "REG_BABY02" &&
            w.WaitingListStatus == WaitingListStatusEnum.Waiting &&
            w.PlacedBedId == string.Empty &&
            w.PlacedPakaiBedId == string.Empty
        )), Times.Once);

        // Assert: SignalR dispatched to baby's ward BSL11
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BSL11", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVoidedRoomingInBabyDirectly_AndBabyHasNoWaitingList_CreatesFallbackWaitingListWithRawatGabungNote()
    {
        // Arrange: Direct baby void (fn_tarif = 0)
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 15, 10, 0, 0));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff("REG_BABY03", "MR_BABY03", "By. Ny. C");
        var layanan = new LayananReff("LAY01", "Ranap Mawar");
        var bed = new BedReff("BED01", "Bed 01", true);
        var tipeKamar = new TipeKamarReff("TK01", "Standar", true);
        var kelas = new KelasReff("KL01", "Kelas 1");
        var auditTrail = new AuditTrailType(masuk, AuditInfoType.Default, AuditInfoType.Default);

        var babyBed = new PakaiBedModel(
            "PKB_BABY03",
            periode,
            reg,
            layanan,
            bed,
            tipeKamar,
            kelas,
            0m, // Rooming-in baby has 0 tariff
            auditTrail);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BABY03")))
            .Returns(MayBe.From(babyBed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked("REG_BABY03"))
            .Returns(false);

        var now = new DateTime(2026, 11, 5, 12, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG_BABY03"))
            .Returns(Enumerable.Empty<WaitingListModel>());

        var regRepoMock = new Mock<IRegRepo>();
        var babyReg = new RegModel(
            "REG_BABY03",
            new DateOnly(2026, 11, 4),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("MR_BABY03", "By. Ny. C", new DateOnly(2026, 11, 4), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            new KelasReff("KL01", "Kelas 1"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff("DOK01", "dr. Anak"),
            new LayananReff("LAY01", "Ranap Mawar"),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            [],
            bangsal: new BangsalReff("LAY01", "Ranap Mawar"),
            jenisInap: new JenisInapType("3", "Perinatal"),
            regIbu: "REG_MOTHER03");

        regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BABY03")))
            .Returns(MayBe.From(babyReg));

        var sut = new PakaiBedDeleteCommandHandler(
            _pakaiBedRepoMock.Object,
            _auditRepoMock.Object,
            _tglJamProviderMock.Object,
            _billingLockCheckerMock.Object,
            _pakaiBedDalMock.Object,
            _tindakanRepoMock.Object,
            _waitingListRepoMock.Object,
            regRepo: regRepoMock.Object,
            bedOperationalRepo: _bedOperationalRepoMock.Object,
            eventPublisher: _eventPublisherMock.Object,
            emr20Gateway: _emr20GatewayMock.Object);

        var cmd = new PakaiBedVoidCommand("PKB_BABY03", "nurse_v", "Batal gabung", "Mozilla/5.0", "127.0.0.1");

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert: Fallback waiting list created with note "Batal Check-in Rawat Gabung"
        _waitingListRepoMock.Verify(x => x.Save(It.Is<WaitingListModel>(w =>
            w.RegId == "REG_BABY03" &&
            w.WaitingListStatus == WaitingListStatusEnum.Waiting &&
            w.SpecialRequirements == "Batal Check-in Rawat Gabung" &&
            w.Bangsal.BangsalId == "LAY01"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedVoided_CallsEmr20GatewayDischargeRegRanapAsyncWithIsGabungFalse()
    {
        // Arrange
        var bed = CreateTestBed("PKB001", "REG-VOID-001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var cmd = new PakaiBedVoidCommand("PKB001", "nurse_v", "Batal check-in", "Mozilla/5.0", "127.0.0.1");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _emr20GatewayMock.Verify(x => x.DischargeRegRanapAsync("REG-VOID-001", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelScopeIsBayi_UnbindsRoomingInBaby_PreservesMotherActiveInBed_AndRevertsBabyWaitingList()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_MOTHER_SCOPE", "REG_MOTHER_SCOPE", "LAY01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_MOTHER_SCOPE")))
            .Returns(MayBe.From(motherBed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked(It.IsAny<string>()))
            .Returns(false);

        var now = new DateTime(2026, 11, 10, 8, 30, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        _pakaiBedDalMock
            .Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB_MOTHER_SCOPE"))
            .ReturnsAsync(new List<string> { "REG_BABY_SCOPE" });

        _pakaiBedDalMock
            .Setup(x => x.VoidRoomingInBabyAsync("PKB_MOTHER_SCOPE", "nurse_v", "Batal Bayi Saja", now))
            .ReturnsAsync(new List<string> { "BSL11" });

        var babyWl = WaitingListModel.Create(
            "REG_BABY_SCOPE",
            new PasienReff("MR_BABY_S", "By. Ny. Scope", new DateOnly(2026, 11, 9), "P"),
            new KelasReff("KL01", "Kelas 1"),
            new BangsalReff("BSL11", "Bangsal Perinatologi"),
            0,
            "admin",
            now.AddHours(-3))
            .Place("BED01", "PKB_BABY_SCOPE", "admin", now.AddHours(-1));

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG_BABY_SCOPE"))
            .Returns(new[] { babyWl });

        var cmd = new PakaiBedVoidCommand("PKB_MOTHER_SCOPE", "nurse_v", "Batal Bayi Saja", "Mozilla/5.0", "127.0.0.1", CancelScope: "bayi");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert 1: Mother's PakaiBed is NOT voided
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.Is<PakaiBedModel>(m => m.PakaiBedId == "PKB_MOTHER_SCOPE" && m.AuditTrail.IsVoided)), Times.Never);

        // Assert 2: DAL VoidRoomingInBabyAsync was called
        _pakaiBedDalMock.Verify(x => x.VoidRoomingInBabyAsync("PKB_MOTHER_SCOPE", "nurse_v", "Batal Bayi Saja", now), Times.Once);

        // Assert 3: Baby waiting list reverted to Waiting
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(w =>
            w.RegId == "REG_BABY_SCOPE" &&
            w.WaitingListStatus == WaitingListStatusEnum.Waiting
        )), Times.Once);

        // Assert 4: Bed operational was NOT marked CleaningRequired (Mother still active)
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Never);

        // Assert 5: EMR Gateway called with isGabung: true
        _emr20GatewayMock.Verify(x => x.DischargeRegRanapAsync("REG_BABY_SCOPE", true, It.IsAny<CancellationToken>()), Times.Once);

        // Assert 6: SignalR broadcast Occupied to mother's bed & WaitingList to baby's ward
        _eventPublisherMock.Verify(x => x.PublishBedStatusChangedAsync("LAY01", "BED01", "Occupied", null, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("LAY01", It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishWaitingListUpdatedAsync("BSL11", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelScopeIsBayi_AndBabyHasActiveTindakan_ThrowsInvalidOperationException_ZeroTransactionInvariant()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_MOTHER_TDK", "REG_MOTHER_TDK", "LAY01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_MOTHER_TDK")))
            .Returns(MayBe.From(motherBed));

        _pakaiBedDalMock
            .Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB_MOTHER_TDK"))
            .ReturnsAsync(new List<string> { "REG_BABY_TDK" });

        var tindakanMock = new TindakanView(
            "TDK-001",
            DateTime.Now,
            "ORD-001",
            new RegReff("REG_BABY_TDK", "PAS_B", "By"),
            new LayananReff("LAY01", "Mawar"),
            new TarifReff("TRF01", "Pemeriksaan Neonatus"));

        _tindakanRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BABY_TDK")))
            .Returns(new[] { tindakanMock });

        var cmd = new PakaiBedVoidCommand("PKB_MOTHER_TDK", "nurse_v", "Batal Bayi", "Mozilla/5.0", "127.0.0.1", CancelScope: "bayi");

        // Act & Assert
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tindakan bayi harus dibatalkan/void terlebih dahulu sebelum membatalkan penempatan.*");
    }

    [Fact]
    public async Task Handle_WhenCancelScopeIsKeduanya_VoidsBedAndUnbindsRoomingInBaby()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_MOTHER_ALL", "REG_MOTHER_ALL", "LAY01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_MOTHER_ALL")))
            .Returns(MayBe.From(motherBed));

        _billingLockCheckerMock
            .Setup(x => x.CheckIsBillingLocked(It.IsAny<string>()))
            .Returns(false);

        var now = new DateTime(2026, 11, 10, 9, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        _pakaiBedDalMock
            .Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB_MOTHER_ALL"))
            .ReturnsAsync(new List<string> { "REG_BABY_ALL" });

        var babyWl = WaitingListModel.Create(
            "REG_BABY_ALL",
            new PasienReff("MR_BABY_ALL", "By. Ny. All", new DateOnly(2026, 11, 9), "P"),
            new KelasReff("KL01", "Kelas 1"),
            new BangsalReff("BSL11", "Bangsal Perinatologi"),
            0,
            "admin",
            now.AddHours(-3))
            .Place("BED01", "PKB_BABY_ALL", "admin", now.AddHours(-1));

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG_BABY_ALL"))
            .Returns(new[] { babyWl });

        var cmd = new PakaiBedVoidCommand("PKB_MOTHER_ALL", "nurse_v", "Batal Keduanya", "Mozilla/5.0", "127.0.0.1", CancelScope: "keduanya");

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert 1: Mother's PakaiBed is voided
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.Is<PakaiBedModel>(m => m.PakaiBedId == "PKB_MOTHER_ALL" && m.AuditTrail.IsVoided)), Times.Once);

        // Assert 2: DAL VoidBedAndRoomChargeAsync was called
        _pakaiBedDalMock.Verify(x => x.VoidBedAndRoomChargeAsync("PKB_MOTHER_ALL", "nurse_v", "Batal Keduanya", now), Times.Once);

        // Assert 3: Baby waiting list reverted to Waiting
        _waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(w =>
            w.RegId == "REG_BABY_ALL" &&
            w.WaitingListStatus == WaitingListStatusEnum.Waiting
        )), Times.Once);

        // Assert 4: Bed operational marked CleaningRequired (entire bed vacated)
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(b =>
            b.CurrentReadiness == BedReadinessStatusEnum.CleaningRequired
        )), Times.Once);
    }
}
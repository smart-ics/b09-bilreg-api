using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.Integration;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.RoomChargeFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedReleaseCommandTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<IEmr20AntrianRanapGateway> _emr20GatewayMock = new();
    private readonly PakaiBedReleaseCommandHandler _sut;

    public PakaiBedReleaseCommandTest()
    {
        _sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            emr20Gateway: _emr20GatewayMock.Object);
    }

    private static PakaiBedModel CreateTestBed(string pakaiBedId = "PKB001")
    {
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 15, 10, 0, 0));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff("REG001", "PAS001", "Pasien A");
        var layanan = new LayananReff("LAY01", "Ranap Mawar");
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
    public async Task Handle_WhenBedFound_ReleasesBedAndDispatchesReconcileCheckoutCmd()
    {
        // Arrange
        var bed = CreateTestBed("PKB001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var checkoutTime = new DateTime(2026, 10, 19, 14, 0, 0);
        var cmd = new PakaiBedReleaseCommand("PKB001", checkoutTime, "nurse_user", "Discharged");

        var reconcileResponse = new RoomChargeReconcileResponse("RC001", 1.0m, 300000m);
        RoomChargeReconcileCheckoutCmd? capturedReconcileCmd = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<RoomChargeReconcileResponse>, CancellationToken>((req, _) => capturedReconcileCmd = (RoomChargeReconcileCheckoutCmd)req)
            .ReturnsAsync(reconcileResponse);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().Be("PKB001");
        result.TglJamOut.Should().Be(checkoutTime);
        result.ReconcileResult.Should().Be(reconcileResponse);

        // Verify PakaiBed was released and saved
        bed.Periode.Keluar.UserId.Should().Be("nurse_user");
        bed.Periode.Keluar.Timestamp.Should().Be(checkoutTime);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(bed), Times.Once);

        // Verify RoomChargeReconcileCheckoutCmd was dispatched via mediator
        capturedReconcileCmd.Should().NotBeNull();
        capturedReconcileCmd!.PakaiBedId.Should().Be("PKB001");
        capturedReconcileCmd.CheckOutTime.Should().Be(checkoutTime);
        capturedReconcileCmd.UserId.Should().Be("nurse_user");

        // Verify Emr20AntrianRanapGateway was notified with isGabung=false
        _emr20GatewayMock.Verify(x => x.DischargeRegRanapAsync("REG001", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTglJamOutDefault_UsesTglJamProviderNow()
    {
        // Arrange
        var bed = CreateTestBed("PKB001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var now = new DateTime(2026, 10, 19, 15, 30, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(now);

        var cmd = new PakaiBedReleaseCommand("PKB001", default, "nurse_user");

        var reconcileResponse = new RoomChargeReconcileResponse("RC002", 0.5m, 150000m);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconcileResponse);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.TglJamOut.Should().Be(now);
        bed.Periode.Keluar.Timestamp.Should().Be(now);
        _mediatorMock.Verify(x => x.Send(
            It.Is<RoomChargeReconcileCheckoutCmd>(c => c.CheckOutTime == now),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBed>()))
            .Returns(MayBe<PakaiBedModel>.None);

        var cmd = new PakaiBedReleaseCommand("NONEXISTENT", DateTime.UtcNow, "nurse1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NONEXISTENT*");

        _mediatorMock.Verify(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPakaiBedIdEmpty_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("", DateTime.UtcNow, "nurse1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenBatalCheckIn_RevertsWaitingListToWaitingAndVoidsPakaiBed()
    {
        // Arrange
        var bed = CreateTestBed("PKB001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var wl = WaitingListModel.Create(
            "REG001",
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new KelasReff("KL01", "Kelas 1"),
            new BangsalReff("B01", "Bangsal Mawar"),
            1,
            "usr_in"
        ).Place("BED01", "PKB001", "usr_in");

        waitingListRepoMock.Setup(x => x.ListByRegId("REG001")).Returns(new[] { wl });

        var sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            waitingListRepoMock.Object);

        var checkoutTime = new DateTime(2026, 10, 19, 14, 0, 0);
        var cmd = new PakaiBedReleaseCommand("PKB001", checkoutTime, "nurse_user", "Batal Check-in: Salah pilih nomor bed");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoomChargeReconcileResponse("RC001", 0m, 0m));

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsBatalCheckIn.Should().BeTrue();
        result.RevertedWaitingListBangsalId.Should().Be("B01");
        bed.AuditTrail.Voided.UserId.Should().Be("nurse_user");
        waitingListRepoMock.Verify(x => x.SaveChanges(It.Is<WaitingListModel>(m =>
            m.WaitingListStatus == WaitingListStatusEnum.Waiting &&
            m.ArrivalSourceType == ArrivalSourceTypeEnum.Adm &&
            m.PlacedBedId == string.Empty &&
            m.PlacedPakaiBedId == string.Empty
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBatalCheckIn_AndHasActiveTindakan_ThrowsInvalidOperationException()
    {
        // Arrange
        var bed = CreateTestBed("PKB001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var tindakanRepoMock = new Mock<ITindakanRepo>();
        var activeTindakan = new TindakanJualView(
            "TDK001",
            new DateTime(2026, 10, 16, 10, 0, 0),
            "ORD001",
            new RegReff("REG001", "PAS001", "Pasien A"),
            new LayananReff("LAY01", "Ranap Mawar"),
            "TRF01",
            "Injeksi Ceftriaxone",
            1,
            50000m,
            "TINDAKAN",
            "PKB001");

        tindakanRepoMock
            .Setup(x => x.ListDataTdkJual(It.Is<IRegKey>(r => r.RegId == "REG001")))
            .Returns(new[] { activeTindakan });

        var sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            tindakanRepo: tindakanRepoMock.Object);

        var checkoutTime = new DateTime(2026, 10, 19, 14, 0, 0);
        var cmd = new PakaiBedReleaseCommand("PKB001", checkoutTime, "nurse_user", "Batal Check-in: Pasien salah bed");

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Batal pemakaian bed tidak diizinkan karena pasien masih memiliki tindakan aktif*");

        _pakaiBedRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCheckoutTimeEarlierThanCheckInOnSameDate_NormalizesToCheckInTime()
    {
        // Arrange
        var bed = CreateTestBed("PKB001"); // Check-in: 2026-10-15 10:00:00
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoomChargeReconcileResponse(null, 1.0m, 300000m));

        // Checkout time earlier on the same date (e.g. 09:30 vs checkin 10:00)
        var checkoutTime = new DateTime(2026, 10, 15, 9, 30, 0);
        var cmd = new PakaiBedReleaseCommand("PKB001", checkoutTime, "nurse_user", "Izin Dokter");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        bed.Periode.Keluar.Timestamp.Should().Be(bed.Periode.Masuk.Timestamp);
        _mediatorMock.Verify(x => x.Send(It.Is<RoomChargeReconcileCheckoutCmd>(c =>
            c.CheckOutTime == bed.Periode.Masuk.Timestamp), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCheckoutDatePriorToCheckInDate_ThrowsInvalidOperationException()
    {
        // Arrange
        var bed = CreateTestBed("PKB001"); // Check-in: 2026-10-15 10:00:00
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var checkoutTime = new DateTime(2026, 10, 14, 10, 0, 0); // Prior calendar date
        var cmd = new PakaiBedReleaseCommand("PKB001", checkoutTime, "nurse_user", "Izin Dokter");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Waktu keluar tidak boleh lebih awal dari waktu masuk*");
    }

    [Fact]
    public async Task Handle_WhenDischarged_SetsBedOperationalToCleaningRequired()
    {
        // Arrange
        var bed = CreateTestBed("PKB001");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(bed));

        var bedRepoMock = new Mock<IBedRepo>();
        var bedType = new BedType("BED01", "Bed 01", new KamarReff("KMR01", "Kamar 01"), new BangsalReff("BGS01", "Bangsal 01"), true);
        bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01"))).Returns(MayBe.From(bedType));

        var bedOpRepoMock = new Mock<IBedOperationalRepo>();
        bedOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>())).Returns(MayBe<BedOperationalModel>.None);

        var sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            bedOperationalRepo: bedOpRepoMock.Object,
            bedRepo: bedRepoMock.Object);

        var checkoutTime = new DateTime(2026, 10, 19, 14, 0, 0);
        var cmd = new PakaiBedReleaseCommand("PKB001", checkoutTime, "nurse_user", "Pasien sembuh");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.BedId.Should().Be("BED01");
        result.BangsalId.Should().Be("BGS01");
        bedOpRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == Bilreg.Domain.BedUsageContext.BedOperationalFeature.BedReadinessStatusEnum.CleaningRequired
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDischarged_SetsBedOperationalWithCheckoutReasonAndDirtySince()
    {
        // Arrange
        var bed = CreateTestBed("PKB002");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB002")))
            .Returns(MayBe.From(bed));

        var bedRepoMock = new Mock<IBedRepo>();
        var bedType = new BedType("BED01", "Bed 01", new KamarReff("KMR01", "Kamar 01"), new BangsalReff("BGS01", "Bangsal 01"), true);
        bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01"))).Returns(MayBe.From(bedType));

        var bedOpRepoMock = new Mock<IBedOperationalRepo>();
        bedOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>())).Returns(MayBe<BedOperationalModel>.None);

        var sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            bedOperationalRepo: bedOpRepoMock.Object,
            bedRepo: bedRepoMock.Object);

        var checkoutTime = new DateTime(2026, 10, 20, 15, 30, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedReleaseCommand("PKB002", checkoutTime, "nurse_john", "Rujuk ke RS Luar");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        bedOpRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == Bilreg.Domain.BedUsageContext.BedOperationalFeature.BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Rujuk ke RS Luar" &&
                t.ResponsibleActorId == "nurse_john" &&
                t.OccurredAt == checkoutTime)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBatalCheckIn_SetsBedOperationalToCleaningRequired_InsteadOfReady()
    {
        // Arrange
        var bed = CreateTestBed("PKB003");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB003")))
            .Returns(MayBe.From(bed));

        var bedRepoMock = new Mock<IBedRepo>();
        var bedType = new BedType("BED01", "Bed 01", new KamarReff("KMR01", "Kamar 01"), new BangsalReff("BGS01", "Bangsal 01"), true);
        bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01"))).Returns(MayBe.From(bedType));

        var bedOpRepoMock = new Mock<IBedOperationalRepo>();
        bedOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>())).Returns(MayBe<BedOperationalModel>.None);

        var sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            bedOperationalRepo: bedOpRepoMock.Object,
            bedRepo: bedRepoMock.Object);

        var checkoutTime = new DateTime(2026, 10, 20, 16, 0, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedReleaseCommand("PKB003", checkoutTime, "nurse_john", "Pembersihan Pasca Batal Check-In: Pasien salah kamar");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsBatalCheckIn.Should().BeTrue();
        bedOpRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == Bilreg.Domain.BedUsageContext.BedOperationalFeature.BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Pembersihan Pasca Batal Check-In: Pasien salah kamar" &&
                t.ResponsibleActorId == "nurse_john" &&
                t.SourceFactId == "BATAL-PKB003")
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBatalCheckIn_WithoutStandardPrefix_AutomaticallyPrependsStandardCleaningPrefix()
    {
        // Arrange
        var bed = CreateTestBed("PKB004");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB004")))
            .Returns(MayBe.From(bed));

        var bedRepoMock = new Mock<IBedRepo>();
        var bedType = new BedType("BED01", "Bed 01", new KamarReff("KMR01", "Kamar 01"), new BangsalReff("BGS01", "Bangsal 01"), true);
        bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01"))).Returns(MayBe.From(bedType));

        var bedOpRepoMock = new Mock<IBedOperationalRepo>();
        bedOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>())).Returns(MayBe<BedOperationalModel>.None);

        var sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            bedOperationalRepo: bedOpRepoMock.Object,
            bedRepo: bedRepoMock.Object);

        var checkoutTime = new DateTime(2026, 10, 20, 16, 0, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedReleaseCommand("PKB004", checkoutTime, "nurse_john", "Batal check-in: Salah ruangan");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsBatalCheckIn.Should().BeTrue();
        bedOpRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == Bilreg.Domain.BedUsageContext.BedOperationalFeature.BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Pembersihan Pasca Batal Check-In: Batal check-in: Salah ruangan" &&
                t.ResponsibleActorId == "nurse_john" &&
                t.SourceFactId == "BATAL-PKB004")
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBatalCheckIn_WithVariedBatalReason_SetsBedOperationalToCleaningRequired_WithStandardPrefix()
    {
        // Arrange
        var bed = CreateTestBed("PKB005");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB005")))
            .Returns(MayBe.From(bed));

        var bedRepoMock = new Mock<IBedRepo>();
        var bedType = new BedType("BED01", "Bed 01", new KamarReff("KMR01", "Kamar 01"), new BangsalReff("BGS01", "Bangsal 01"), true);
        bedRepoMock.Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01"))).Returns(MayBe.From(bedType));

        var bedOpRepoMock = new Mock<IBedOperationalRepo>();
        bedOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>())).Returns(MayBe<BedOperationalModel>.None);

        var sut = new PakaiBedReleaseCommandHandler(
            _pakaiBedRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object,
            bedOperationalRepo: bedOpRepoMock.Object,
            bedRepo: bedRepoMock.Object);

        var checkoutTime = new DateTime(2026, 10, 20, 16, 30, 0, DateTimeKind.Utc);
        var cmd = new PakaiBedReleaseCommand("PKB005", checkoutTime, "nurse_sarah", "Batal check-in perawat: Pasien menolak rawat");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsBatalCheckIn.Should().BeTrue();
        bedOpRepoMock.Verify(x => x.SaveChanges(It.Is<BedOperationalModel>(m =>
            m.BedId == "BED01" &&
            m.CurrentReadiness == Bilreg.Domain.BedUsageContext.BedOperationalFeature.BedReadinessStatusEnum.CleaningRequired &&
            m.ListReadinessTransaction.Any(t =>
                t.Reason == "Pembersihan Pasca Batal Check-In: Batal check-in perawat: Pasien menolak rawat" &&
                t.ResponsibleActorId == "nurse_sarah" &&
                t.SourceFactId == "BATAL-PKB005")
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBedReleased_CallsEmr20GatewayDischargeRegRanapAsyncWithIsGabungFalse()
    {
        // Arrange
        var bed = CreateTestBed("PKB-DISCHARGE-01");
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB-DISCHARGE-01")))
            .Returns(MayBe.From(bed));

        var checkoutTime = new DateTime(2026, 10, 19, 14, 0, 0);
        var cmd = new PakaiBedReleaseCommand("PKB-DISCHARGE-01", checkoutTime, "nurse_user", "Discharged");

        var reconcileResponse = new RoomChargeReconcileResponse("RC001", 1.0m, 300000m);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RoomChargeReconcileCheckoutCmd>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconcileResponse);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _emr20GatewayMock.Verify(x => x.DischargeRegRanapAsync("REG001", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIdEmpty_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new PakaiBedReleaseCommand("PKB001", DateTime.UtcNow, "");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}

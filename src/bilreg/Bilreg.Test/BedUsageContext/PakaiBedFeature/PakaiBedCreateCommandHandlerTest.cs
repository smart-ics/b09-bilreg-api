using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedCreateCommandHandlerTest
{
    private readonly Mock<IPakaiBedAlokasiRepo> _alokasiRepoMock = new();
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<IPakaiBedActiveOccupancyDal> _activeOccupancyDalMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<ITipeKamarRepo> _tipeKamarRepoMock = new();
    private readonly Mock<IRoomRateRepo> _roomRateRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();

    private static readonly DateTime CheckInTimeUtc = new(2026, 10, 16, 10, 0, 0, DateTimeKind.Utc);

    private PakaiBedCreateCommandHandler CreateSut() =>
        new(
            _alokasiRepoMock.Object,
            _bedOperationalRepoMock.Object,
            _activeOccupancyDalMock.Object,
            _regRepoMock.Object,
            _bedRepoMock.Object,
            _layananRepoMock.Object,
            _kamarRepoMock.Object,
            _tipeKamarRepoMock.Object,
            _roomRateRepoMock.Object,
            _kelasRepoMock.Object);

    [Fact]
    public async Task Handle_NormalClinicalAllocation_Success()
    {
        // Arrange
        SetupMasterDefaults();
        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc);

        PakaiBedAlokasiModel? savedAlokasi = null;
        PakaiBedModel? savedLegacy = null;

        _alokasiRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedAlokasiModel, PakaiBedModel>((a, l) =>
            {
                savedAlokasi = a;
                savedLegacy = l;
            });

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().StartWith("BE");

        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.PakaiBedId.Should().Be(result.PakaiBedId);
        savedAlokasi.RegId.Should().Be("REG-001");
        savedAlokasi.PasienId.Should().Be("PAS-001");
        savedAlokasi.BedId.Should().Be("BED-01");
        savedAlokasi.BangsalId.Should().Be("BG-01");
        savedAlokasi.KamarId.Should().Be("KMR-01");
        savedAlokasi.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Active);
        savedAlokasi.PakaiBedPurpose.Should().Be(PakaiBedPurposeEnum.Clinical);
        savedAlokasi.OccupantRole.Should().Be(OccupantRoleEnum.Primary);
        savedAlokasi.WaitingListId.Should().Be("-");
        savedAlokasi.PhysicalKelasId.Should().Be("KL3");
        savedAlokasi.BillingKelasId.Should().Be("KL3");
        savedAlokasi.Tarif.Should().Be(500000m);
        savedAlokasi.IsTitipKelas.Should().BeFalse();
        savedAlokasi.IsNaikKelas.Should().BeFalse();
        savedAlokasi.MotherPakaiBedId.Should().BeEmpty();
        savedAlokasi.ListTransition.Should().HaveCount(2);

        savedLegacy.Should().NotBeNull();
        savedLegacy!.PakaiBedId.Should().Be(result.PakaiBedId);
        savedLegacy.Kelas.KelasId.Should().Be("KL3");
        savedLegacy.Tarif.Should().Be(500000m);

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TitipKelas_Success_StoresDualClassAndSetsTitipFlag()
    {
        // Arrange
        SetupMasterDefaults();
        var billingKelas = new KelasType("KL1", "KELAS 1", true, KelasDkType.Default);
        _kelasRepoMock.Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KL1")))
            .Returns(MayBe.From(billingKelas));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            BillingKelasId: "KL1");

        PakaiBedAlokasiModel? savedAlokasi = null;
        PakaiBedModel? savedLegacy = null;

        _alokasiRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedAlokasiModel, PakaiBedModel>((a, l) =>
            {
                savedAlokasi = a;
                savedLegacy = l;
            });

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.PakaiBedId.Should().StartWith("BE");
        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.PhysicalKelasId.Should().Be("KL3");
        savedAlokasi.BillingKelasId.Should().Be("KL1");
        savedAlokasi.IsTitipKelas.Should().BeTrue();
        savedAlokasi.IsNaikKelas.Should().BeFalse();

        savedLegacy.Should().NotBeNull();
        savedLegacy!.Kelas.KelasId.Should().Be("KL1");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NaikKelas_Success_SetsNaikKelasFlag()
    {
        // Arrange
        SetupMasterDefaults();
        var billingKelas = new KelasType("VIP", "VIP", true, KelasDkType.Default);
        _kelasRepoMock.Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "VIP")))
            .Returns(MayBe.From(billingKelas));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            BillingKelasId: "VIP",
            IsNaikKelas: true);

        PakaiBedAlokasiModel? savedAlokasi = null;

        _alokasiRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedAlokasiModel, PakaiBedModel>((a, l) => savedAlokasi = a);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.PakaiBedId.Should().StartWith("BE");
        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.PhysicalKelasId.Should().Be("KL3");
        savedAlokasi.BillingKelasId.Should().Be("VIP");
        savedAlokasi.IsNaikKelas.Should().BeTrue();
        savedAlokasi.IsTitipKelas.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithExplicitWaitingListId_PreservesWaitingListId()
    {
        // Arrange
        SetupMasterDefaults();
        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            WaitingListId: "WTL-999");

        PakaiBedAlokasiModel? savedAlokasi = null;

        _alokasiRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedAlokasiModel, PakaiBedModel>((a, l) => savedAlokasi = a);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.WaitingListId.Should().Be("WTL-999");
    }

    [Fact]
    public async Task Handle_BedAlreadyOccupied_ThrowsInvalidOperationException()
    {
        // Arrange
        _activeOccupancyDalMock.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns(new PakaiBedActiveOccupancyResponse(
                PakaiBedId: "BE26J150001",
                RegId: "REG-OTHER",
                PasienId: "PAS-OTHER",
                PasienName: "OTHER",
                BangsalId: "BG-01",
                BangsalName: "BANGSAL MAWAR",
                KamarId: "KMR-01",
                KamarName: "KAMAR 101",
                BedId: "BED-01",
                BedName: "BED 01A",
                TglJamMasuk: CheckInTimeUtc.AddDays(-1),
                BillingKelasId: "KL3",
                BillingKelasName: "KELAS 3",
                IsRoomingIn: false,
                MotherPakaiBedId: string.Empty));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            IsRoomingIn: false);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sedang aktif ditempati pasien lain*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BedNotReady_CleaningRequired_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupMasterDefaults();
        var dirtyBedOp = BedOperationalModel.Create(
            "BED-01", "BG-01", "KMR-01",
            new OccupancyPolicyReff("POL-1", "Standard inpatient"))
            .RecordReadiness(
                BedReadinessStatusEnum.CleaningRequired,
                BedRestrictionTypeEnum.Cleaning,
                "Need cleaning",
                string.Empty,
                "USR-1",
                CheckInTimeUtc.AddHours(-1),
                CheckInTimeUtc.AddHours(-1).AddMinutes(1),
                string.Empty,
                "REQ-DIRTY");

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(dirtyBedOp));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Readiness Bed BED-01 belum terverifikasi Ready*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BedNotReady_MaintenanceBlocked_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupMasterDefaults();
        var maintenanceBedOp = BedOperationalModel.Create(
            "BED-01", "BG-01", "KMR-01",
            new OccupancyPolicyReff("POL-1", "Standard inpatient"))
            .RecordReadiness(
                BedReadinessStatusEnum.OutOfService,
                BedRestrictionTypeEnum.Maintenance,
                "Broken frame",
                string.Empty,
                "USR-1",
                CheckInTimeUtc.AddHours(-2),
                CheckInTimeUtc.AddHours(-2).AddMinutes(1),
                string.Empty,
                "REQ-MAINT");

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(maintenanceBedOp));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoomingIn_Success_LinksMotherAndSetsTarifZero()
    {
        // Arrange
        SetupMasterDefaults();

        // Bed is occupied by the mother!
        _activeOccupancyDalMock.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns(new PakaiBedActiveOccupancyResponse(
                PakaiBedId: "BE-MOM-12345",
                RegId: "REG-MOM",
                PasienId: "PAS-MOM",
                PasienName: "IBU MAWAR",
                BangsalId: "BG-01",
                BangsalName: "BANGSAL MAWAR",
                KamarId: "KMR-01",
                KamarName: "KAMAR 101",
                BedId: "BED-01",
                BedName: "BED 01A",
                TglJamMasuk: CheckInTimeUtc.AddDays(-1),
                BillingKelasId: "KL3",
                BillingKelasName: "KELAS 3",
                IsRoomingIn: false,
                MotherPakaiBedId: string.Empty));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            MotherPakaiBedId: "BE-MOM-12345",
            IsRoomingIn: true);

        PakaiBedAlokasiModel? savedAlokasi = null;
        PakaiBedModel? savedLegacy = null;

        _alokasiRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()))
            .Callback<PakaiBedAlokasiModel, PakaiBedModel>((a, l) =>
            {
                savedAlokasi = a;
                savedLegacy = l;
            });

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.PakaiBedId.Should().StartWith("BE");

        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.PakaiBedPurpose.Should().Be(PakaiBedPurposeEnum.RoomingIn);
        savedAlokasi.OccupantRole.Should().Be(OccupantRoleEnum.Associated);
        savedAlokasi.MotherPakaiBedId.Should().Be("BE-MOM-12345");
        savedAlokasi.Tarif.Should().Be(0);
        savedAlokasi.IsRoomingIn.Should().BeTrue();

        savedLegacy.Should().NotBeNull();
        savedLegacy!.Tarif.Should().Be(0);

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RoomingIn_WithoutMotherPakaiBedId_ThrowsInvalidOperationException()
    {
        // Arrange
        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            MotherPakaiBedId: "",
            IsRoomingIn: true);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rooming-in memerlukan MotherPakaiBedId*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoomingIn_BedEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        _activeOccupancyDalMock.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns((PakaiBedActiveOccupancyResponse?)null);

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            MotherPakaiBedId: "BE-MOM-12345",
            IsRoomingIn: true);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak sedang aktif ditempati alokasi ibu*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoomingIn_BedOccupiedByOtherPatient_ThrowsInvalidOperationException()
    {
        // Arrange
        _activeOccupancyDalMock.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns(new PakaiBedActiveOccupancyResponse(
                PakaiBedId: "BE-OTHER-9999",
                RegId: "REG-OTHER",
                PasienId: "PAS-OTHER",
                PasienName: "OTHER",
                BangsalId: "BG-01",
                BangsalName: "BANGSAL MAWAR",
                KamarId: "KMR-01",
                KamarName: "KAMAR 101",
                BedId: "BED-01",
                BedName: "BED 01A",
                TglJamMasuk: CheckInTimeUtc.AddDays(-1),
                BillingKelasId: "KL3",
                BillingKelasName: "KELAS 3",
                IsRoomingIn: false,
                MotherPakaiBedId: string.Empty));

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc,
            MotherPakaiBedId: "BE-MOM-12345",
            IsRoomingIn: true);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sedang ditempati alokasi BE-OTHER-9999, bukan alokasi ibu BE-MOM-12345*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>(), It.IsAny<PakaiBedModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegNotFound_ThrowsException()
    {
        // Arrange
        _activeOccupancyDalMock.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns((PakaiBedActiveOccupancyResponse?)null);
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-UNKNOWN",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Reg not found*");
    }

    [Fact]
    public async Task Handle_BedNotFound_ThrowsException()
    {
        // Arrange
        _activeOccupancyDalMock.Setup(x => x.GetActiveByBed("BED-01"))
            .Returns((PakaiBedActiveOccupancyResponse?)null);
        SetupReg();
        _bedRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedKey>()))
            .Returns(MayBe<BedType>.None);

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-UNKNOWN",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Bed not found*");
    }

    [Fact]
    public async Task Handle_BedOperationalNotFound_ThrowsException()
    {
        // Arrange
        SetupMasterDefaults();
        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedOperationalKey>()))
            .Returns(MayBe<BedOperationalModel>.None);

        var cmd = new PakaiBedCreateCommand(
            RegId: "REG-001",
            LayananId: "LAY-01",
            TipeKamarId: "TK-01",
            BedId: "BED-01",
            UserId: "USR-NURSE",
            TglJamIn: CheckInTimeUtc);

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Bed Operational BED-01 not found*");
    }

    #region Helper Methods

    private void SetupMasterDefaults()
    {
        _activeOccupancyDalMock.Setup(x => x.GetActiveByBed(It.IsAny<string>()))
            .Returns((PakaiBedActiveOccupancyResponse?)null);

        SetupReg();
        SetupBed();
        SetupKamar();
        SetupLayanan();
        SetupTipeKamar();
        SetupKelas();
        SetupRoomRate();
        SetupBedOperationalReady();
    }

    private void SetupReg()
    {
        var reg = new RegModel(
            "REG-001",
            new DateOnly(2026, 10, 16),
            new AuditInfoType("USR-NURSE", CheckInTimeUtc),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("PAS-001", "PASIEN MAWAR", new DateOnly(1995, 5, 20), "P"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            new KelasReff("KL3", "KELAS 3"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff("LAY-01", "RANAP MAWAR"),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));
    }

    private void SetupBed()
    {
        var bed = new BedType(
            "BED-01",
            "BED 01A",
            new KamarReff("KMR-01", "KAMAR 101"),
            new BangsalReff("BG-01", "BANGSAL MAWAR"),
            true);

        _bedRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBedKey>()))
            .Returns(MayBe.From(bed));
    }

    private void SetupKamar()
    {
        var kamar = new KamarType(
            "KMR-01",
            "KAMAR 101",
            new BangsalReff("BG-01", "BANGSAL MAWAR"),
            new KelasReff("KL3", "KELAS 3"));

        _kamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe.From(kamar));
    }

    private void SetupLayanan()
    {
        var layanan = new LayananType(
            "LAY-01",
            "RANAP MAWAR",
            true,
            InstalasiType.Default.ToReff(),
            LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default,
            InstalasiDkType.Default,
            new UnitReff("-", "-"),
            new PoliBpjsReff("-", "-"));

        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe.From(layanan));
    }

    private void SetupTipeKamar()
    {
        var tipeKamar = new TipeKamarType(
            "TK-01",
            "STANDAR",
            true,
            false,
            true);

        _tipeKamarRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITipeKamarKey>()))
            .Returns(MayBe.From(tipeKamar));
    }

    private void SetupKelas()
    {
        var kelas = new KelasType(
            "KL3",
            "KELAS 3",
            true,
            KelasDkType.Default);

        _kelasRepoMock.Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KL3")))
            .Returns(MayBe.From(kelas));
    }

    private void SetupRoomRate()
    {
        var detailMock = new Mock<IRoomRateDetail>();
        detailMock.SetupGet(x => x.TipeKamar).Returns(new TipeKamarReff("TK-01", "STANDAR", true));
        detailMock.SetupGet(x => x.TotalNilai).Returns(500000m);

        var roomRateMock = new Mock<IRoomRate<IRoomRateDetail>>();
        roomRateMock.SetupGet(x => x.Kamar).Returns(new KamarReff("KMR-01", "KAMAR 101"));
        roomRateMock.SetupGet(x => x.ListTipe).Returns(new[] { detailMock.Object });

        _roomRateRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKamarKey>()))
            .Returns(MayBe.From(roomRateMock.Object));
    }

    private void SetupBedOperationalReady()
    {
        var readyBedOp = BedOperationalModel.Create(
            "BED-01",
            "BG-01",
            "KMR-01",
            new OccupancyPolicyReff("POL-1", "Standard inpatient"))
            .VerifyReady(
                "Bed verified clean and ready",
                "EVD-READY-001",
                "USR-HK",
                "VERIFIER-HK",
                CheckInTimeUtc.AddHours(-1),
                CheckInTimeUtc.AddHours(-1).AddMinutes(1),
                string.Empty,
                "REQ-VERIFY-001");

        _bedOperationalRepoMock.Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED-01")))
            .Returns(MayBe.From(readyBedOp));
    }

    #endregion
}

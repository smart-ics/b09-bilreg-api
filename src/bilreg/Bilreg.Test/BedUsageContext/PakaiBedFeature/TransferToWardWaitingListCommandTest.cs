using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
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
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class TransferToWardWaitingListCommandTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock = new();
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();
    private readonly Mock<IBedRepo> _bedRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IBedOperationalRepo> _bedOperationalRepoMock = new();
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly TransferToWardWaitingListCommandHandler _sut;

    public TransferToWardWaitingListCommandTest()
    {
        _sut = new TransferToWardWaitingListCommandHandler(
            _pakaiBedRepoMock.Object,
            _bangsalRepoMock.Object,
            _bedRepoMock.Object,
            _regRepoMock.Object,
            _bedOperationalRepoMock.Object,
            _waitingListRepoMock.Object,
            _tglJamProviderMock.Object);

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 11, 3, 10, 0, 0));
    }

    private static PakaiBedModel CreateTestBed(
        string pakaiBedId = "PKB001",
        string bedId = "BED01",
        string regId = "REG001",
        decimal tarif = 300000m,
        string pasienId = "PAS001",
        string pasienName = "Pasien A")
    {
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 15, 10, 0, 0));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff(regId, pasienId, pasienName);
        var layanan = new LayananReff("LAY01", "Ranap Mawar");
        var bed = new BedReff(bedId, "Bed 01", true);
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
            tarif,
            auditTrail);
    }

    private static RegModel CreateTestBabyReg(string regId = "REG_BAYI", string motherRegId = "REG_IBU")
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 10, 16),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("PAS_BAYI", "Bayi Ny A", new DateOnly(2026, 10, 16), "L"),
            new TipeJaminanReff("TJ01", "Umum"),
            new PolisReff("-", "-", "-"),
            new KelasReff("KL01", "Kelas 1"),
            CaraMasukDkType.Default,
            new RujukanReff("-", "-"),
            new PpaReff("DOK01", "dr. Subroto"),
            new LayananReff("LAY01", "Ranap Mawar"),
            new KarcisReff("-", "-"),
            new RegEligibilityType("-", "-", "-"),
            [],
            regIbu: motherRegId);
    }

    private static RegModel CreateTestReg(string regId = "REG001")
    {
        return new RegModel(
            regId,
            new DateOnly(2026, 10, 15),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new TipeJaminanReff("TJ01", "Umum"),
            new PolisReff("-", "-", "-"),
            new KelasReff("KL01", "Kelas 1"),
            CaraMasukDkType.Default,
            new RujukanReff("-", "-"),
            new PpaReff("DOK01", "dr. Subroto"),
            new LayananReff("LAY01", "Ranap Mawar"),
            new KarcisReff("-", "-"),
            new RegEligibilityType("-", "-", "-"),
            []);
    }

    [Fact]
    public async Task Handle_ShouldReleaseOldBed_SetOperationalToCleaningRequired_AndCreateWaitingList_WhenInputIsValid()
    {
        // Arrange
        var pakaiBed = CreateTestBed("PKB001", "BED01", "REG001");
        var reg = CreateTestReg("REG001");
        var targetBangsal = BangsalType.Default with { BangsalId = "BGS02", BangsalName = "Bangsal Melati" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };
        var oldBedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(pakaiBed));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS02")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(reg));

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBedOp));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("REG001"))
            .Returns(MayBe<WaitingListModel>.None);

        BedOperationalModel? savedBedOp = null;
        _bedOperationalRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(model => savedBedOp = model);

        WaitingListModel? savedWl = null;
        _waitingListRepoMock
            .Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(model => savedWl = model);

        var transferTime = new DateTime(2026, 11, 3, 10, 30, 0);
        var cmd = new TransferToWardWaitingListCommand(
            "PKB001",
            "BGS02",
            transferTime,
            "Kebutuhan ICU",
            "Pasien perlu monitoring ketat",
            "nurse_1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().Be("PKB001");
        result.OldBedId.Should().Be("BED01");
        result.OldBangsalId.Should().Be("BGS01");
        result.TargetBangsalId.Should().Be("BGS02");
        result.TglJamTransfer.Should().Be(transferTime);
        result.Status.Should().Be("Waiting");
        result.WaitingListId.Should().NotBeNullOrWhiteSpace();

        // 1. Verify old bed released
        pakaiBed.Periode.Keluar.UserId.Should().Be("nurse_1");
        pakaiBed.Periode.Keluar.Timestamp.Should().Be(transferTime);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(pakaiBed), Times.Once);

        // 2. Verify old bed marked CleaningRequired
        savedBedOp.Should().NotBeNull();
        savedBedOp!.CurrentReadiness.Should().Be(BedReadinessStatusEnum.CleaningRequired);
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Once);

        // 3. Verify target waiting list created
        savedWl.Should().NotBeNull();
        savedWl!.RegId.Should().Be("REG001");
        savedWl.Bangsal.BangsalId.Should().Be("BGS02");
        savedWl.Bangsal.BangsalName.Should().Be("Bangsal Melati");
        savedWl.ArrivalSourceType.Should().Be(ArrivalSourceTypeEnum.TransferInternal);
        savedWl.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        savedWl.SpecialRequirements.Should().Contain("Kebutuhan ICU");
        savedWl.SpecialRequirements.Should().Contain("Pasien perlu monitoring ketat");
        savedWl.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Urgent);
        _waitingListRepoMock.Verify(x => x.Save(It.IsAny<WaitingListModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenPakaiBedNotFound()
    {
        // Arrange
        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPakaiBed>()))
            .Returns(MayBe<PakaiBedModel>.None);

        var cmd = new TransferToWardWaitingListCommand(
            "NON_EXISTENT",
            "BGS02",
            DateTime.Now,
            "Transfer",
            null,
            "nurse_1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenTargetBangsalIsSameAsCurrentBangsal()
    {
        // Arrange
        var pakaiBed = CreateTestBed("PKB001", "BED01", "REG001");
        var targetBangsal = BangsalType.Default with { BangsalId = "BGS01", BangsalName = "Bangsal Mawar" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(pakaiBed));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS01")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        var cmd = new TransferToWardWaitingListCommand(
            "PKB001",
            "BGS01",
            DateTime.Now,
            "Pindah internal",
            null,
            "nurse_1");

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Transfer antar-bangsal harus memilih bangsal yang berbeda*");
    }

    [Fact]
    public async Task Handle_ShouldRerouteActiveWaitingList_WhenPreviousActiveWaitingListExists()
    {
        // Arrange
        var pakaiBed = CreateTestBed("PKB001", "BED01", "REG001");
        var reg = CreateTestReg("REG001");
        var targetBangsal = BangsalType.Default with { BangsalId = "BGS02", BangsalName = "Bangsal Melati" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };
        var oldBedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default);

        var existingActiveWl = WaitingListModel.Create(
            "REG001",
            reg.Pasien,
            reg.Kelas,
            new BangsalReff("BGS03", "Bangsal Kenanga"),
            ArrivalSourceTypeEnum.Adm,
            new LayananReff("-", "-"),
            new DokterReff("-", "-"),
            ClinicalUrgencyEnum.Routine,
            "",
            0,
            "nurse_0");

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(pakaiBed));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS02")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(reg));

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBedOp));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("REG001"))
            .Returns(MayBe.From(existingActiveWl));

        WaitingListModel? reroutedWl = null;
        _waitingListRepoMock
            .Setup(x => x.SaveChanges(It.Is<WaitingListModel>(w => w.WaitingListId == existingActiveWl.WaitingListId)))
            .Callback<WaitingListModel>(m => reroutedWl = m);

        var cmd = new TransferToWardWaitingListCommand(
            "PKB001",
            "BGS02",
            new DateTime(2026, 11, 3, 11, 0, 0),
            "Alih rawat",
            null,
            "nurse_1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        reroutedWl.Should().NotBeNull();
        reroutedWl!.WaitingListStatus.Should().Be(WaitingListStatusEnum.Rerouted);
    }

    [Fact]
    public async Task Handle_ShouldPreserveCitoUrgencyFromPriorWaitingList_WhenAlasanTransferDoesNotContainCitoKeyword()
    {
        // Arrange
        var pakaiBed = CreateTestBed("PKB001", "BED01", "REG001");
        var reg = CreateTestReg("REG001");
        var targetBangsal = BangsalType.Default with { BangsalId = "BGS02", BangsalName = "Bangsal Melati" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };
        var oldBedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default);

        // Previous waiting list for this patient was Cito (Emergency)
        var priorCitoWl = WaitingListModel.Create(
            "REG001",
            reg.Pasien,
            reg.Kelas,
            new BangsalReff("BGS01", "Bangsal Mawar"),
            ArrivalSourceTypeEnum.Igd,
            new LayananReff("IGD", "Instalasi Gawat Darurat"),
            new DokterReff("-", "-"),
            ClinicalUrgencyEnum.Emergency,
            "Cito dari IGD",
            2,
            "nurse_0");

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(pakaiBed));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS02")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(reg));

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBedOp));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("REG001"))
            .Returns(MayBe<WaitingListModel>.None);

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG001"))
            .Returns(new List<WaitingListModel> { priorCitoWl });

        WaitingListModel? savedWl = null;
        _waitingListRepoMock
            .Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => savedWl = m);

        // Transfer with ordinary reason (does not contain "Cito")
        var cmd = new TransferToWardWaitingListCommand(
            "PKB001",
            "BGS02",
            new DateTime(2026, 11, 3, 11, 0, 0),
            "Permintaan pindah kamar",
            null,
            "nurse_1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedWl.Should().NotBeNull();
        savedWl!.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Emergency);
        savedWl.Priority.Should().Be(2);
        savedWl.PreviousWaitListId.Should().Be(priorCitoWl.WaitingListId);
    }

    [Fact]
    public async Task Handle_ShouldUseExplicitClinicalUrgency_WhenProvidedInRequest()
    {
        // Arrange
        var pakaiBed = CreateTestBed("PKB001", "BED01", "REG001");
        var reg = CreateTestReg("REG001");
        var targetBangsal = BangsalType.Default with { BangsalId = "BGS02", BangsalName = "Bangsal Melati" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };
        var oldBedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(pakaiBed));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS02")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(reg));

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBedOp));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("REG001"))
            .Returns(MayBe<WaitingListModel>.None);

        _waitingListRepoMock
            .Setup(x => x.ListByRegId("REG001"))
            .Returns(new List<WaitingListModel>());

        WaitingListModel? savedWl = null;
        _waitingListRepoMock
            .Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(m => savedWl = m);

        var cmd = new TransferToWardWaitingListCommand(
            "PKB001",
            "BGS02",
            new DateTime(2026, 11, 3, 11, 0, 0),
            "Alih rawat",
            null,
            "nurse_1",
            ClinicalUrgency: "Cito");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedWl.Should().NotBeNull();
        savedWl!.ClinicalUrgency.Should().Be(ClinicalUrgencyEnum.Emergency);
        savedWl.Priority.Should().Be(2);
    }

    [Fact]
    public async Task Handle_MotherScope_WithRoomingInBaby_CreatesWaitingListForBothMotherAndBabyAndMarksOldBedDirty()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_IBU", "BED01", "REG_IBU", 300000m);
        var babyBed = CreateTestBed("PKB_BAYI", "BED01", "REG_BAYI", 0m, "PAS_BAYI", "Bayi Ny A");
        var motherReg = CreateTestReg("REG_IBU");
        var babyReg = CreateTestBabyReg("REG_BAYI", "REG_IBU");

        var targetBangsal = BangsalType.Default with { BangsalId = "BGS02", BangsalName = "Bangsal Melati" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };
        var oldBedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_IBU")))
            .Returns(MayBe.From(motherBed));

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BAYI")))
            .Returns(MayBe.From(babyBed));

        _pakaiBedRepoMock
            .Setup(x => x.GetRoomingInBabyTrsIdsAsync("BED01", "REG_BAYI"))
            .ReturnsAsync(new List<string> { "PKB_BAYI" });

        _pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BAYI")))
            .Returns(new List<PakaiBedModel> { babyBed });

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS02")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_IBU")))
            .Returns(MayBe.From(motherReg));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BAYI")))
            .Returns(MayBe.From(babyReg));

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBedOp));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId(It.IsAny<string>()))
            .Returns(MayBe<WaitingListModel>.None);

        BedOperationalModel? savedBedOp = null;
        _bedOperationalRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<BedOperationalModel>()))
            .Callback<BedOperationalModel>(model => savedBedOp = model);

        var savedWls = new List<WaitingListModel>();
        _waitingListRepoMock
            .Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(model => savedWls.Add(model));

        var transferTime = new DateTime(2026, 11, 3, 11, 0, 0);
        var cmd = new TransferToWardWaitingListCommand(
            "PKB_IBU",
            "BGS02",
            transferTime,
            "Pindah bangsal rawat inap",
            "Catatan transfer",
            "nurse_1",
            TransferScope: "ibu",
            BabyRegId: "REG_BAYI");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeTrue();
        result.WaitingListId.Should().NotBeNullOrWhiteSpace();
        result.BabyWaitingListId.Should().NotBeNullOrWhiteSpace();
        result.WaitingListId.Should().NotBe(result.BabyWaitingListId);

        // Old bed released for mother
        motherBed.Periode.Keluar.Timestamp.Should().Be(transferTime);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(motherBed), Times.Once);

        // Old bed released for baby
        babyBed.Periode.Keluar.Timestamp.Should().Be(transferTime);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(babyBed), Times.Once);
        _pakaiBedRepoMock.Verify(x => x.ReleaseRoomingInBabyAsync("BED01", "REG_BAYI", "nurse_1", transferTime), Times.Once);

        // Old bed marked CleaningRequired
        savedBedOp.Should().NotBeNull();
        savedBedOp!.CurrentReadiness.Should().Be(BedReadinessStatusEnum.CleaningRequired);

        // Two waiting list entries saved
        savedWls.Should().HaveCount(2);
        savedWls.Should().Contain(w => w.RegId == "REG_IBU");
        savedWls.Should().Contain(w => w.RegId == "REG_BAYI");
    }

    [Fact]
    public async Task Handle_BabyScope_InvokedFromMotherBed_CreatesWaitingListOnlyForBabyAndDoesNotMarkOldBedDirty()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_IBU", "BED01", "REG_IBU", 300000m);
        var babyBed = CreateTestBed("PKB_BAYI", "BED01", "REG_BAYI", 0m, "PAS_BAYI", "Bayi Ny A");
        var motherReg = CreateTestReg("REG_IBU");
        var babyReg = CreateTestBabyReg("REG_BAYI", "REG_IBU");

        var targetBangsal = BangsalType.Default with { BangsalId = "BGS_NICU", BangsalName = "Bangsal NICU" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_IBU")))
            .Returns(MayBe.From(motherBed));

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BAYI")))
            .Returns(MayBe.From(babyBed));

        _pakaiBedRepoMock
            .Setup(x => x.GetRoomingInBabyTrsIdsAsync("BED01", "REG_BAYI"))
            .ReturnsAsync(new List<string> { "PKB_BAYI" });

        _pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BAYI")))
            .Returns(new List<PakaiBedModel> { babyBed });

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS_NICU")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BAYI")))
            .Returns(MayBe.From(babyReg));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("REG_BAYI"))
            .Returns(MayBe<WaitingListModel>.None);

        var savedWls = new List<WaitingListModel>();
        _waitingListRepoMock
            .Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(model => savedWls.Add(model));

        var transferTime = new DateTime(2026, 11, 3, 11, 0, 0);
        var cmd = new TransferToWardWaitingListCommand(
            "PKB_IBU",
            "BGS_NICU",
            transferTime,
            "Transfer bayi ke NICU",
            "Perlu inkubator",
            "nurse_1",
            TransferScope: "bayi",
            BabyRegId: "REG_BAYI");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeFalse();
        result.WaitingListId.Should().NotBeNullOrWhiteSpace();
        result.BabyWaitingListId.Should().Be(result.WaitingListId);

        // Mother bed was NOT released
        motherBed.Periode.Keluar.Timestamp.Should().Be(new DateTime(3000, 1, 1));
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(motherBed), Times.Never);

        // Old bed operational was NOT set to cleaning
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Never);

        // Baby bed was released
        babyBed.Periode.Keluar.Timestamp.Should().Be(transferTime);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(babyBed), Times.Once);
        _pakaiBedRepoMock.Verify(x => x.ReleaseRoomingInBabyAsync("BED01", "REG_BAYI", "nurse_1", transferTime), Times.Once);

        // Waiting list created ONLY for baby
        savedWls.Should().HaveCount(1);
        savedWls[0].RegId.Should().Be("REG_BAYI");
        savedWls[0].Bangsal.BangsalId.Should().Be("BGS_NICU");
    }

    [Fact]
    public async Task Handle_BabyScope_InvokedDirectlyWithBabyPakaiBedId_CreatesWaitingListOnlyForBabyAndDoesNotMarkOldBedDirty()
    {
        // Arrange
        var babyBed = CreateTestBed("PKB_BAYI", "BED01", "REG_BAYI", 0m, "PAS_BAYI", "Bayi Ny A");
        var babyReg = CreateTestBabyReg("REG_BAYI", "REG_IBU");

        var targetBangsal = BangsalType.Default with { BangsalId = "BGS_NICU", BangsalName = "Bangsal NICU" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_BAYI")))
            .Returns(MayBe.From(babyBed));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS_NICU")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BAYI")))
            .Returns(MayBe.From(babyReg));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("REG_BAYI"))
            .Returns(MayBe<WaitingListModel>.None);

        var savedWls = new List<WaitingListModel>();
        _waitingListRepoMock
            .Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(model => savedWls.Add(model));

        var transferTime = new DateTime(2026, 11, 3, 11, 0, 0);
        var cmd = new TransferToWardWaitingListCommand(
            "PKB_BAYI",
            "BGS_NICU",
            transferTime,
            "Transfer direct baby to NICU",
            "Kondisi memburuk",
            "nurse_1",
            TransferScope: "bayi");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeFalse();
        result.WaitingListId.Should().NotBeNullOrWhiteSpace();

        // Old bed operational was NOT set to cleaning
        _bedOperationalRepoMock.Verify(x => x.SaveChanges(It.IsAny<BedOperationalModel>()), Times.Never);

        // Baby bed released
        babyBed.Periode.Keluar.Timestamp.Should().Be(transferTime);
        _pakaiBedRepoMock.Verify(x => x.SaveChanges(babyBed), Times.Once);

        // Only one waiting list created
        savedWls.Should().HaveCount(1);
        savedWls[0].RegId.Should().Be("REG_BAYI");
    }

    [Fact]
    public void SystemTextJson_Deserialize_WithTransferScopeAndBabyRegId_ShouldSucceed()
    {
        // Arrange
        var json = """
        {
            "pakaiBedId": "PKB001",
            "targetBangsalId": "BGS02",
            "tglJamTransfer": "2026-11-03T10:30:00",
            "alasanTransfer": "Pindah bangsal",
            "catatan": "Catatan",
            "userId": "nurse_1",
            "clinicalUrgency": "Cito",
            "transferScope": "bayi",
            "babyRegId": "REG_BAYI"
        }
        """;

        // Act
        var cmd = System.Text.Json.JsonSerializer.Deserialize<TransferToWardWaitingListCommand>(
            json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        cmd.Should().NotBeNull();
        cmd!.PakaiBedId.Should().Be("PKB001");
        cmd.TargetBangsalId.Should().Be("BGS02");
        cmd.TransferScope.Should().Be("bayi");
        cmd.BabyRegId.Should().Be("REG_BAYI");
        cmd.ClinicalUrgency.Should().Be("Cito");
    }

    [Fact]
    public async Task Handle_BabyScope_WhenNoBabyRecordFoundAndNoBabyRegId_ThrowsInvalidOperationException()
    {
        // Arrange
        var pakaiBed = CreateTestBed("PKB001", "BED01", "REG001");
        var targetBangsal = BangsalType.Default with { BangsalId = "BGS02", BangsalName = "Bangsal Melati" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB001")))
            .Returns(MayBe.From(pakaiBed));

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS02")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _pakaiBedRepoMock
            .Setup(x => x.GetRoomingInBabyTrsIdsAsync("BED01", null))
            .ReturnsAsync(new List<string>());

        _pakaiBedRepoMock
            .Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB001"))
            .ReturnsAsync(new List<string>());

        var cmd = new TransferToWardWaitingListCommand(
            "PKB001",
            "BGS02",
            DateTime.Now,
            "Transfer Bayi Mandiri",
            null,
            "nurse_1",
            TransferScope: "bayi",
            BabyRegId: null);

        // Act & Assert
        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rooming-in baby record not found*");
    }

    [Fact]
    public async Task Handle_MotherScope_WithMultipleRoomingInBabies_CreatesWaitingListForMotherAndAllBabies()
    {
        // Arrange
        var motherBed = CreateTestBed("PKB_IBU", "BED01", "REG_IBU", 300000m);
        var babyBed1 = CreateTestBed("PKB_BAYI1", "BED01", "REG_BAYI1", 0m, "PAS_BAYI1", "Bayi Ny A 1");
        var babyBed2 = CreateTestBed("PKB_BAYI2", "BED01", "REG_BAYI2", 0m, "PAS_BAYI2", "Bayi Ny A 2");
        var motherReg = CreateTestReg("REG_IBU");
        var babyReg1 = CreateTestBabyReg("REG_BAYI1", "REG_IBU");
        var babyReg2 = CreateTestBabyReg("REG_BAYI2", "REG_IBU");

        var targetBangsal = BangsalType.Default with { BangsalId = "BGS02", BangsalName = "Bangsal Melati" };
        var oldBed = BedType.Default with
        {
            BedId = "BED01",
            BedName = "Bed 01",
            Bangsal = new BangsalReff("BGS01", "Bangsal Mawar"),
            Kamar = new KamarReff("KMR01", "Kamar Mawar 1"),
            IsAktif = true
        };
        var oldBedOp = BedOperationalModel.Create("BED01", "BGS01", "KMR01", OccupancyPolicyReff.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "PKB_IBU")))
            .Returns(MayBe.From(motherBed));

        _pakaiBedRepoMock
            .Setup(x => x.GetRoomingInBabyRegIdsAsync("PKB_IBU"))
            .ReturnsAsync(new List<string> { "REG_BAYI1", "REG_BAYI2" });

        _pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BAYI1")))
            .Returns(new List<PakaiBedModel> { babyBed1 });
        _pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG_BAYI2")))
            .Returns(new List<PakaiBedModel> { babyBed2 });

        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "BGS02")))
            .Returns(MayBe.From(targetBangsal));

        _bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBed));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_IBU")))
            .Returns(MayBe.From(motherReg));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BAYI1")))
            .Returns(MayBe.From(babyReg1));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_BAYI2")))
            .Returns(MayBe.From(babyReg2));

        _bedOperationalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedOperationalKey>(k => k.BedId == "BED01")))
            .Returns(MayBe.From(oldBedOp));

        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId(It.IsAny<string>()))
            .Returns(MayBe<WaitingListModel>.None);

        var savedWls = new List<WaitingListModel>();
        _waitingListRepoMock
            .Setup(x => x.Save(It.IsAny<WaitingListModel>()))
            .Callback<WaitingListModel>(model => savedWls.Add(model));

        var transferTime = new DateTime(2026, 11, 3, 11, 0, 0);
        var cmd = new TransferToWardWaitingListCommand(
            "PKB_IBU",
            "BGS02",
            transferTime,
            "Pindah bangsal ibu dan bayi kembar",
            null,
            "nurse_1",
            TransferScope: "ibu");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsCleaningTriggered.Should().BeTrue();

        // 3 waiting list records created: Mother, Baby 1, Baby 2
        savedWls.Should().HaveCount(3);
        savedWls.Should().Contain(w => w.RegId == "REG_IBU");
        savedWls.Should().Contain(w => w.RegId == "REG_BAYI1");
        savedWls.Should().Contain(w => w.RegId == "REG_BAYI2");
    }
}

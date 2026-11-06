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

    private static PakaiBedModel CreateTestBed(string pakaiBedId = "PKB001", string bedId = "BED01", string regId = "REG001")
    {
        var masuk = new AuditInfoType("usr_in", new DateTime(2026, 10, 15, 10, 0, 0));
        var periode = new PeriodePakaiBedType(masuk, AuditInfoType.Default);
        var reg = new RegReff(regId, "PAS001", "Pasien A");
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
            300000m,
            auditTrail);
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
}

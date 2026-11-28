using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmGetAdmissionHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IRegInapRepo> _regInapRepoMock = new();
    private readonly AdmGetAdmissionHandler _sut;

    public AdmGetAdmissionHandlerTest()
    {
        _sut = new AdmGetAdmissionHandler(
            _admissionRepoMock.Object,
            _regRepoMock.Object,
            _regInapRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAdmissionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM999")))
            .Returns(MayBe<AdmissionModel>.None);

        var query = new AdmGetAdmissionQry("ADM999");

        // Act
        Func<Task> act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Admission 'ADM999' tidak ditemukan.");
    }

    [Fact]
    public async Task Handle_WhenRegAndRegInapExist_MapsCaraMasukDpjpAndProsedur()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM001");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM001")))
            .Returns(MayBe.From(admission));

        var reg = CreateSampleReg("ADM001", caraMasukDkId: "8", dokterId: "REG_DOK",
            tipeJaminanId: "TJ01", tipeJaminanName: "BPJS Kesehatan",
            polisId: "POL01", noPolis: "000123456",
            jenisInapId: "1", regIbu: "REG-IBU-001");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM001")))
            .Returns(MayBe.From(reg));

        var regInap = CreateSampleRegInap("ADM001", prosedurMasukInapId: "1", dpjpId: "DPJP_AKTIF");
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM001")))
            .Returns(MayBe.From(regInap));

        var query = new AdmGetAdmissionQry("ADM001");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("ADM001");
        result.AdmissionStatus.Should().Be(AdmissionStatusEnum.Admitted);
        result.Pasien.PasienId.Should().Be("PAS001");
        result.CaraMasukDkId.Should().Be("8");
        result.DokterId.Should().Be("DPJP_AKTIF");
        result.ProsedurMasukInapId.Should().Be("1");
        result.TipeJaminanId.Should().Be("TJ01");
        result.TipeJaminanName.Should().Be("BPJS Kesehatan");
        result.PolisId.Should().Be("POL01");
        result.NoPolis.Should().Be("000123456");
        result.JenisInapId.Should().Be("1");
        result.RegIbu.Should().Be("REG-IBU-001");
    }

    [Fact]
    public async Task Handle_WhenRegInapHasNoDpjp_FallsBackToRegDokter()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM002");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM002")))
            .Returns(MayBe.From(admission));

        var reg = CreateSampleReg("ADM002", caraMasukDkId: "2", dokterId: "FALLBACK_DOK");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM002")))
            .Returns(MayBe.From(reg));

        // RegInap with no primary DPJP or empty DPJP
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM002")))
            .Returns(MayBe<RegInapModel>.None);

        var query = new AdmGetAdmissionQry("ADM002");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.CaraMasukDkId.Should().Be("2");
        result.DokterId.Should().Be("FALLBACK_DOK");
        result.ProsedurMasukInapId.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRegAndRegInapNotFound_ReturnsEmptyEnrichedFields()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM003");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM003")))
            .Returns(MayBe.From(admission));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM003")))
            .Returns(MayBe<RegModel>.None);

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM003")))
            .Returns(MayBe<RegInapModel>.None);

        var query = new AdmGetAdmissionQry("ADM003");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.CaraMasukDkId.Should().BeEmpty();
        result.DokterId.Should().BeEmpty();
        result.ProsedurMasukInapId.Should().BeEmpty();
        result.TipeJaminanId.Should().BeEmpty();
        result.TipeJaminanName.Should().BeEmpty();
        result.PolisId.Should().BeEmpty();
        result.NoPolis.Should().BeEmpty();
        result.JenisInapId.Should().BeEmpty();
        result.RegIbu.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenFieldsContainLegacyDash_NormalizesToEmpty()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM004");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM004")))
            .Returns(MayBe.From(admission));

        var reg = CreateSampleReg("ADM004", caraMasukDkId: "-", dokterId: "-",
            tipeJaminanId: "-", tipeJaminanName: "-", polisId: "-", noPolis: "-",
            jenisInapId: "-", regIbu: "-");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM004")))
            .Returns(MayBe.From(reg));

        var regInap = CreateSampleRegInap("ADM004", prosedurMasukInapId: "-", dpjpId: "-");
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM004")))
            .Returns(MayBe.From(regInap));

        var query = new AdmGetAdmissionQry("ADM004");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.CaraMasukDkId.Should().BeEmpty();
        result.DokterId.Should().BeEmpty();
        result.ProsedurMasukInapId.Should().BeEmpty();
        result.TipeJaminanId.Should().BeEmpty();
        result.TipeJaminanName.Should().BeEmpty();
        result.PolisId.Should().BeEmpty();
        result.NoPolis.Should().BeEmpty();
        result.JenisInapId.Should().BeEmpty();
        result.RegIbu.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRegHasJenisInapAndRegIbu_MapsBothProperties()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM005");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM005")))
            .Returns(MayBe.From(admission));

        var reg = CreateSampleReg("ADM005", "8", "REG_DOK", jenisInapId: "3", regIbu: "REG_IBU_123");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM005")))
            .Returns(MayBe.From(reg));

        var regInap = CreateSampleRegInap("ADM005", "1", "DPJP_AKTIF");
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM005")))
            .Returns(MayBe.From(regInap));

        var query = new AdmGetAdmissionQry("ADM005");

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.JenisInapId.Should().Be("3");
        result.RegIbu.Should().Be("REG_IBU_123");
    }

    [Fact]
    public async Task Handle_WhenMotherHasActivePakaiBed_ResolvesCurrentLocationFromLatestActivePakaiBed()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM006");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM006")))
            .Returns(MayBe.From(admission));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM006")))
            .Returns(MayBe<RegModel>.None);
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM006")))
            .Returns(MayBe<RegInapModel>.None);

        var pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var bedRepoMock = new Mock<IBedRepo>();
        var kelasRepoMock = new Mock<IKelasRepo>();

        var checkedOutBed = CreateSamplePakaiBed(
            "PB001", "BED_OLD", "KLS_OLD",
            new DateTime(2026, 7, 7, 10, 0, 0),
            new DateTime(2026, 7, 8, 9, 0, 0));
        var voidedActiveBed = CreateSamplePakaiBed(
            "PB002", "BED_VOID", "KLS_VOID",
            new DateTime(2026, 7, 9, 12, 0, 0),
            new DateTime(3000, 1, 1));
        voidedActiveBed.Void("USER01", new DateTime(2026, 7, 9, 12, 5, 0));

        var currentActiveBed = CreateSamplePakaiBed(
            "PB003", "BED_NEW", "KLS_NEW",
            new DateTime(2026, 7, 8, 9, 5, 0),
            new DateTime(3000, 1, 1));

        pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "ADM006")))
            .Returns([checkedOutBed, voidedActiveBed, currentActiveBed]);

        bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED_NEW")))
            .Returns(MayBe.From(new BedType(
                "BED_NEW", "Bed Baru",
                new KamarReff("KMR02", "Kamar 2"),
                new BangsalReff("B02", "Bangsal Mawar"),
                true)));

        kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KLS_NEW")))
            .Returns(MayBe.From(new KelasType(
                "KLS_NEW", "VIP", true, new KelasDkType("KDK02", "Kelas VIP"))));

        var resolver = new AdmissionCurrentLocationResolver(
            pakaiBedRepoMock.Object,
            waitingListRepoMock.Object,
            bedRepoMock.Object,
            kelasRepoMock.Object);
        var sut = new AdmGetAdmissionHandler(
            _admissionRepoMock.Object,
            _regRepoMock.Object,
            _regInapRepoMock.Object,
            resolver);

        // Act
        var result = await sut.Handle(new AdmGetAdmissionQry("ADM006"), CancellationToken.None);

        // Assert
        result.KelasDk.Should().Be(new KelasDkType("KDK01", "Kelas 1"));
        result.Bangsal.Should().Be(new BangsalReff("B01", "Bangsal A"));
        result.CurrentKelasDk.Should().Be(new KelasDkType("KDK02", "Kelas VIP"));
        result.CurrentBangsal.Should().Be(new BangsalReff("B02", "Bangsal Mawar"));
    }

    [Fact]
    public async Task Handle_WhenNoActivePakaiBedButHasActiveWaitingList_ResolvesCurrentLocationFromWaitingList()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM007");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM007")))
            .Returns(MayBe.From(admission));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM007")))
            .Returns(MayBe<RegModel>.None);
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM007")))
            .Returns(MayBe<RegInapModel>.None);

        var pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var bedRepoMock = new Mock<IBedRepo>();
        var kelasRepoMock = new Mock<IKelasRepo>();

        pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "ADM007")))
            .Returns(Array.Empty<PakaiBedModel>());

        var waitingList = WaitingListModel.Create(
            "ADM007",
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "P"),
            new KelasReff("KLS_WL", "Kelas II"),
            new BangsalReff("B03", "Bangsal Melati"),
            1,
            "USER01",
            new DateTime(2026, 7, 7, 10, 5, 0));

        waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("ADM007"))
            .Returns(MayBe.From(waitingList));

        kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KLS_WL")))
            .Returns(MayBe.From(new KelasType(
                "KLS_WL", "Kelas II", true, new KelasDkType("KDK03", "Kelas 2"))));

        var resolver = new AdmissionCurrentLocationResolver(
            pakaiBedRepoMock.Object,
            waitingListRepoMock.Object,
            bedRepoMock.Object,
            kelasRepoMock.Object);
        var sut = new AdmGetAdmissionHandler(
            _admissionRepoMock.Object,
            _regRepoMock.Object,
            _regInapRepoMock.Object,
            resolver);

        // Act
        var result = await sut.Handle(new AdmGetAdmissionQry("ADM007"), CancellationToken.None);

        // Assert
        result.KelasDk.Should().Be(new KelasDkType("KDK01", "Kelas 1"));
        result.Bangsal.Should().Be(new BangsalReff("B01", "Bangsal A"));
        result.CurrentKelasDk.Should().Be(new KelasDkType("KDK03", "Kelas 2"));
        result.CurrentBangsal.Should().Be(new BangsalReff("B03", "Bangsal Melati"));
    }

    [Fact]
    public async Task Handle_WhenNoActivePakaiBedOrWaitingList_FallsBackToAdmissionLocation()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM008");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM008")))
            .Returns(MayBe.From(admission));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM008")))
            .Returns(MayBe<RegModel>.None);
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM008")))
            .Returns(MayBe<RegInapModel>.None);

        var pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var bedRepoMock = new Mock<IBedRepo>();
        var kelasRepoMock = new Mock<IKelasRepo>();

        pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "ADM008")))
            .Returns(Array.Empty<PakaiBedModel>());
        waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("ADM008"))
            .Returns(MayBe<WaitingListModel>.None);

        var resolver = new AdmissionCurrentLocationResolver(
            pakaiBedRepoMock.Object,
            waitingListRepoMock.Object,
            bedRepoMock.Object,
            kelasRepoMock.Object);
        var sut = new AdmGetAdmissionHandler(
            _admissionRepoMock.Object,
            _regRepoMock.Object,
            _regInapRepoMock.Object,
            resolver);

        // Act
        var result = await sut.Handle(new AdmGetAdmissionQry("ADM008"), CancellationToken.None);

        // Assert
        result.KelasDk.Should().Be(new KelasDkType("KDK01", "Kelas 1"));
        result.Bangsal.Should().Be(new BangsalReff("B01", "Bangsal A"));
        result.CurrentKelasDk.Should().Be(new KelasDkType("KDK01", "Kelas 1"));
        result.CurrentBangsal.Should().Be(new BangsalReff("B01", "Bangsal A"));
    }

    [Fact]
    public async Task Handle_WhenActivePakaiBedEntityMissingInBedOrKelasRepo_GracefullyFallsBack()
    {
        // Arrange
        var admission = CreateSampleAdmission("ADM009");
        _admissionRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM009")))
            .Returns(MayBe.From(admission));
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM009")))
            .Returns(MayBe<RegModel>.None);
        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "ADM009")))
            .Returns(MayBe<RegInapModel>.None);

        var pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var bedRepoMock = new Mock<IBedRepo>();
        var kelasRepoMock = new Mock<IKelasRepo>();

        var activeBed = CreateSamplePakaiBed(
            "PB009", "BED_MISSING", "KLS_MISSING",
            new DateTime(2026, 7, 8, 9, 5, 0),
            new DateTime(3000, 1, 1));

        pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "ADM009")))
            .Returns([activeBed]);
        bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED_MISSING")))
            .Returns(MayBe<BedType>.None);
        kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KLS_MISSING")))
            .Returns(MayBe<KelasType>.None);
        waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("ADM009"))
            .Returns(MayBe<WaitingListModel>.None);

        var resolver = new AdmissionCurrentLocationResolver(
            pakaiBedRepoMock.Object,
            waitingListRepoMock.Object,
            bedRepoMock.Object,
            kelasRepoMock.Object);
        var sut = new AdmGetAdmissionHandler(
            _admissionRepoMock.Object,
            _regRepoMock.Object,
            _regInapRepoMock.Object,
            resolver);

        // Act
        var result = await sut.Handle(new AdmGetAdmissionQry("ADM009"), CancellationToken.None);

        // Assert
        result.CurrentKelasDk.Should().Be(new KelasDkType("KDK01", "Kelas 1"));
        result.CurrentBangsal.Should().Be(new BangsalReff("B01", "Bangsal A"));
    }

    private static AdmissionModel CreateSampleAdmission(string regId) =>
        new(
            regId,
            AdmissionStatusEnum.Admitted,
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            "-",
            "-",
            new KelasDkType("KDK01", "Kelas 1"),
            new BangsalReff("B01", "Bangsal A"),
            new DateTime(2026, 7, 7, 10, 0, 0),
            AuditTrailType.Create("USER01", new DateTime(2026, 7, 7, 10, 0, 0)));

    private static RegModel CreateSampleReg(
        string regId,
        string caraMasukDkId,
        string dokterId,
        string tipeJaminanId = "-",
        string tipeJaminanName = "-",
        string polisId = "-",
        string noPolis = "-",
        string jenisInapId = "-",
        string regIbu = "") =>
        new(
            regId,
            new DateOnly(2026, 7, 7),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            new TipeJaminanReff(tipeJaminanId, tipeJaminanName),
            new PolisReff(polisId, noPolis, "Pemegang Polis Test"),
            KelasType.Default.ToReff(),
            new CaraMasukDkType(caraMasukDkId, "Cara Masuk Test"),
            RujukanType.Default.ToReff(),
            new PpaReff(dokterId, "Dokter Test"),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [],
            jenisInap: string.IsNullOrEmpty(jenisInapId) ? null : new JenisInapType(jenisInapId, "Jenis Inap Test"),
            regIbu: regIbu);

    private static RegInapModel CreateSampleRegInap(string regId, string prosedurMasukInapId, string dpjpId) =>
        RegInapModel.Create(
            regId,
            new ProsedurMasukInapType(prosedurMasukInapId, "Prosedur Test", ProsedurMasukInapDkType.Default),
            new PpaReff(dpjpId, "DPJP Test"),
            new DateOnly(2026, 7, 7));

    private static PakaiBedModel CreateSamplePakaiBed(
        string pakaiBedId,
        string bedId,
        string kelasId,
        DateTime masukTimestamp,
        DateTime keluarTimestamp) =>
        new(
            pakaiBedId,
            new PeriodePakaiBedType(
                new AuditInfoType("USER01", masukTimestamp),
                new AuditInfoType(keluarTimestamp == new DateTime(3000, 1, 1) ? string.Empty : "USER01", keluarTimestamp)),
            RegModel.Default.ToReff(),
            LayananType.Default.ToReff(),
            new BedReff(bedId, "Bed Test", true),
            TipeKamarType.Default.ToReff(),
            new KelasReff(kelasId, "Kelas Test"),
            0,
            AuditTrailType.Create("USER01", masukTimestamp));
}

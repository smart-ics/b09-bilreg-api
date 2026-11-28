using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
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

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmLookupAdmissionHandlerTest
{
    private readonly Mock<IAdmissionRepo> _admissionRepoMock = new();
    private readonly AdmLookupAdmissionHandler _handler;

    public AdmLookupAdmissionHandlerTest()
    {
        _handler = new AdmLookupAdmissionHandler(_admissionRepoMock.Object);
    }

    [Fact]
    public async Task GivenQueryWithSearch_WhenHandled_ThenPassesSearchFilterToRepoAndMapsResult()
    {
        // Arrange
        var testModel = CreateTestModel("RG00001234", "P0001", "Ny. Pasien Test");
        _admissionRepoMock
            .Setup(x => x.ListData(It.Is<AdmissionListFilter>(f =>
                f.Status == AdmissionStatusEnum.Admitted &&
                f.PasienId == null &&
                f.Search == "006669")))
            .Returns([testModel]);

        var query = new AdmLookupAdmissionQry(
            Status: AdmissionStatusEnum.Admitted,
            PasienId: null,
            Search: "006669");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.RegId.Should().Be("RG00001234");
        item.AdmissionStatus.Should().Be(AdmissionStatusEnum.Admitted);
        item.PasienId.Should().Be("P0001");
        item.PasienName.Should().Be("Ny. Pasien Test");
        item.KelasDkId.Should().Be("1");
        item.KelasDkName.Should().Be("Kelas DK 1");
        item.BangsalId.Should().Be("B001");
        item.BangsalName.Should().Be("Bangsal A");
        item.AdmissionDate.Should().Be(new DateTime(2026, 7, 7));
        item.JenisInapId.Should().Be(InpatientAgePolicy.JENIS_INAP_OBSTETRI);
        item.JenisInapName.Should().Be("Obstetri");

        _admissionRepoMock.Verify(x => x.ListData(It.Is<AdmissionListFilter>(f =>
            f.Status == AdmissionStatusEnum.Admitted &&
            f.PasienId == null &&
            f.Search == "006669")), Times.Once);
    }

    [Fact]
    public async Task GivenQueryWithoutSearch_WhenHandled_ThenSearchFilterDefaultsToNull()
    {
        // Arrange
        _admissionRepoMock
            .Setup(x => x.ListData(It.Is<AdmissionListFilter>(f =>
                f.Status == AdmissionStatusEnum.Admitted &&
                f.PasienId == "PAS01" &&
                f.Search == null)))
            .Returns([]);

        var query = new AdmLookupAdmissionQry(AdmissionStatusEnum.Admitted, "PAS01");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();

        _admissionRepoMock.Verify(x => x.ListData(It.Is<AdmissionListFilter>(f =>
            f.Status == AdmissionStatusEnum.Admitted &&
            f.PasienId == "PAS01" &&
            f.Search == null)), Times.Once);
    }

    [Fact]
    public async Task GivenAdmissionsWithDifferentJenisInap_WhenHandled_ThenMapsJenisInapIdAndNameAccurately()
    {
        // Arrange
        var obstetriAdmission = CreateTestModel("RG00001234", "P0001", "Ny. Obstetri", JenisInapType.Obstetri);
        var umumAdmission = CreateTestModel("RG00001235", "P0002", "Ny. Umum", JenisInapType.Umum);
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([obstetriAdmission, umumAdmission]);

        var query = new AdmLookupAdmissionQry(AdmissionStatusEnum.Admitted);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].JenisInapId.Should().Be("2");
        result.Items[0].JenisInapName.Should().Be("Obstetri");
        result.Items[1].JenisInapId.Should().Be("1");
        result.Items[1].JenisInapName.Should().Be("Umum");
    }

    [Fact]
    public async Task GivenMotherHasActivePakaiBed_WhenHandled_ThenCurrentLocationUsesActivePakaiBedAndKeepsOriginalLocation()
    {
        // Arrange
        var admission = CreateTestModel("RG00001236", "P0003", "Ny. Pindah Bed");
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([admission]);

        var pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var bedRepoMock = new Mock<IBedRepo>();
        var kelasRepoMock = new Mock<IKelasRepo>();

        var activeBed = CreateSamplePakaiBed(
            "PB001", "BED_NEW", "KLS_NEW",
            new DateTime(2026, 7, 8, 9, 0, 0),
            new DateTime(3000, 1, 1));
        pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "RG00001236")))
            .Returns([activeBed]);

        bedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBedKey>(k => k.BedId == "BED_NEW")))
            .Returns(MayBe.From(new BedType(
                "BED_NEW", "Bed Baru",
                new KamarReff("KMR02", "Kamar 2"),
                new BangsalReff("B002", "Bangsal Mawar"),
                true)));

        kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KLS_NEW")))
            .Returns(MayBe.From(new KelasType(
                "KLS_NEW", "VIP", true, new KelasDkType("2", "Kelas VIP"))));

        var resolver = new AdmissionCurrentLocationResolver(
            pakaiBedRepoMock.Object,
            waitingListRepoMock.Object,
            bedRepoMock.Object,
            kelasRepoMock.Object);
        var sut = new AdmLookupAdmissionHandler(_admissionRepoMock.Object, resolver);

        // Act
        var result = await sut.Handle(new AdmLookupAdmissionQry(AdmissionStatusEnum.Admitted), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.KelasDkId.Should().Be("1");
        item.KelasDkName.Should().Be("Kelas DK 1");
        item.BangsalId.Should().Be("B001");
        item.BangsalName.Should().Be("Bangsal A");
        item.CurrentKelasDkId.Should().Be("2");
        item.CurrentKelasDkName.Should().Be("Kelas VIP");
        item.CurrentBangsalId.Should().Be("B002");
        item.CurrentBangsalName.Should().Be("Bangsal Mawar");
    }

    [Fact]
    public async Task GivenMotherHasActiveWaitingList_WhenHandled_ThenCurrentLocationUsesWaitingList()
    {
        // Arrange
        var admission = CreateTestModel("RG00001237", "P0004", "Ny. Waiting List", status: AdmissionStatusEnum.Waiting);
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([admission]);

        var pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var bedRepoMock = new Mock<IBedRepo>();
        var kelasRepoMock = new Mock<IKelasRepo>();

        pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "RG00001237")))
            .Returns(Array.Empty<PakaiBedModel>());

        var waitingList = WaitingListModel.Create(
            "RG00001237",
            new PasienReff("P0004", "Ny. Waiting List", new DateOnly(1990, 5, 15), "P"),
            new KelasReff("KLS_WL", "Kelas II"),
            new BangsalReff("B003", "Bangsal Melati"),
            1,
            "USER01",
            new DateTime(2026, 7, 7, 10, 5, 0));

        waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("RG00001237"))
            .Returns(MayBe.From(waitingList));

        kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KLS_WL")))
            .Returns(MayBe.From(new KelasType(
                "KLS_WL", "Kelas II", true, new KelasDkType("3", "Kelas DK 2"))));

        var resolver = new AdmissionCurrentLocationResolver(
            pakaiBedRepoMock.Object,
            waitingListRepoMock.Object,
            bedRepoMock.Object,
            kelasRepoMock.Object);
        var sut = new AdmLookupAdmissionHandler(_admissionRepoMock.Object, resolver);

        // Act
        var result = await sut.Handle(new AdmLookupAdmissionQry(AdmissionStatusEnum.Waiting), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.KelasDkId.Should().Be("1");
        item.BangsalId.Should().Be("B001");
        item.CurrentKelasDkId.Should().Be("3");
        item.CurrentKelasDkName.Should().Be("Kelas DK 2");
        item.CurrentBangsalId.Should().Be("B003");
        item.CurrentBangsalName.Should().Be("Bangsal Melati");
    }

    [Fact]
    public async Task GivenMotherHasNoActivePakaiBedOrWaitingList_WhenHandled_ThenCurrentLocationFallsBackToAdmission()
    {
        // Arrange
        var admission = CreateTestModel("RG00001238", "P0005", "Ny. Fallback");
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([admission]);

        var pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        var waitingListRepoMock = new Mock<IWaitingListRepo>();
        var bedRepoMock = new Mock<IBedRepo>();
        var kelasRepoMock = new Mock<IKelasRepo>();

        pakaiBedRepoMock
            .Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "RG00001238")))
            .Returns(Array.Empty<PakaiBedModel>());
        waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("RG00001238"))
            .Returns(MayBe<WaitingListModel>.None);

        var resolver = new AdmissionCurrentLocationResolver(
            pakaiBedRepoMock.Object,
            waitingListRepoMock.Object,
            bedRepoMock.Object,
            kelasRepoMock.Object);
        var sut = new AdmLookupAdmissionHandler(_admissionRepoMock.Object, resolver);

        // Act
        var result = await sut.Handle(new AdmLookupAdmissionQry(AdmissionStatusEnum.Admitted), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.CurrentKelasDkId.Should().Be("1");
        item.CurrentKelasDkName.Should().Be("Kelas DK 1");
        item.CurrentBangsalId.Should().Be("B001");
        item.CurrentBangsalName.Should().Be("Bangsal A");
    }

    [Fact]
    public async Task GivenInactiveAdmission_WhenHandled_ThenFallsBackToAdmissionWithoutCallingResolver()
    {
        // Arrange
        var completedAdmission = CreateTestModel(
            "RG00001239", "P0006", "Ny. Completed", status: AdmissionStatusEnum.Completed);
        _admissionRepoMock
            .Setup(x => x.ListData(It.IsAny<AdmissionListFilter>()))
            .Returns([completedAdmission]);

        var resolverMock = new Mock<IAdmissionCurrentLocationResolver>();
        var sut = new AdmLookupAdmissionHandler(_admissionRepoMock.Object, resolverMock.Object);

        // Act
        var result = await sut.Handle(new AdmLookupAdmissionQry(AdmissionStatusEnum.Completed), CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.CurrentKelasDkId.Should().Be("1");
        item.CurrentKelasDkName.Should().Be("Kelas DK 1");
        item.CurrentBangsalId.Should().Be("B001");
        item.CurrentBangsalName.Should().Be("Bangsal A");
        resolverMock.Verify(
            x => x.Resolve(It.IsAny<string>(), It.IsAny<KelasDkType>(), It.IsAny<BangsalReff>()),
            Times.Never);
    }

    private static AdmissionModel CreateTestModel(
        string regId,
        string pasienId,
        string pasienName,
        JenisInapType? jenisInap = null,
        AdmissionStatusEnum status = AdmissionStatusEnum.Admitted) =>
        new(
            regId,
            status,
            new PasienReff(pasienId, pasienName, new DateOnly(1990, 5, 15), "P"),
            "-",
            "-",
            new KelasDkType("1", "Kelas DK 1"),
            new BangsalReff("B001", "Bangsal A"),
            new DateTime(2026, 7, 7),
            AuditTrailType.Create("user1", new DateTime(2026, 7, 7)),
            AdmissionSourceEnum.Legacy,
            "-",
            jenisInap ?? JenisInapType.Obstetri);

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

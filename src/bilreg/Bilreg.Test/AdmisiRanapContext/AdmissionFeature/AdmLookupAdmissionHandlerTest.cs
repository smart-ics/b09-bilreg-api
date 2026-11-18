using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;

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

    private static AdmissionModel CreateTestModel(
        string regId,
        string pasienId,
        string pasienName,
        JenisInapType? jenisInap = null) =>
        new(
            regId,
            AdmissionStatusEnum.Admitted,
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
}

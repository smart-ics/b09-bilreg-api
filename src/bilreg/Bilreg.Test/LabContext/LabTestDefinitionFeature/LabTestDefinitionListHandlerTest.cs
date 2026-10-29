using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabTestDefinitionFeature;

public class LabTestDefinitionListHandlerTest
{
    private readonly Mock<ILabTestDefinitionRepo> _definitionRepoMock = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();

    private static LabTestDefinitionModel CreateDefinition(string id, string tarifId, string tarifName = "Tes") =>
        new(
            id,
            tarifId,
            tarifId,
            tarifName,
            "TEST-CODE",
            tarifName,
            "Darah",
            VacutainerTypeEnum.Edta,
            true,
            AuditTrailType.Default,
            []);

    [Fact]
    public async Task Handle_WithoutNilaiTarifRepo_ReturnsDefinitionsWithZeroPrice()
    {
        // Arrange
        var def = CreateDefinition("LTD0001", "TR-001", "Hemoglobin");
        _definitionRepoMock
            .Setup(r => r.ListData(It.IsAny<LabTestDefinitionListFilter>()))
            .Returns([def]);

        var sut = new LabTestDefinitionListHandler(_definitionRepoMock.Object, null);

        // Act
        var result = (await sut.Handle(new LabTestDefinitionListQuery(true), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TestDefinitionId.Should().Be("LTD0001");
        result[0].TarifPrice.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithNilaiTarifRepo_ResolvesUmumKelas3Price()
    {
        // Arrange
        var def = CreateDefinition("LTD0001", "TR-001", "Hemoglobin");
        _definitionRepoMock
            .Setup(r => r.ListData(It.IsAny<LabTestDefinitionListFilter>()))
            .Returns([def]);

        var nilaiTarif = new NilaiTarifType(
            "NT-001",
            "TR-001",
            "Hemoglobin",
            new TipeTarifReff("UMUM", "Umum"),
            new KelasReff("III", "Kelas III"),
            75000m,
            []);

        _nilaiTarifRepoMock
            .Setup(r => r.LoadEntity(It.Is<INilaiTarifCompositKey>(k =>
                k.TarifId == "TR-001" && k.TipeTarifId == "UMUM" && k.KelasId == "III")))
            .Returns(MayBe.From(nilaiTarif));

        var sut = new LabTestDefinitionListHandler(_definitionRepoMock.Object, _nilaiTarifRepoMock.Object);

        // Act
        var result = (await sut.Handle(new LabTestDefinitionListQuery(true), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TarifPrice.Should().Be(75000m);
    }

    [Fact]
    public async Task Handle_WhenSpecificCompositeNotFound_FallsBackToFirstActiveRate()
    {
        // Arrange
        var def = CreateDefinition("LTD0002", "TR-002", "Leukosit");
        _definitionRepoMock
            .Setup(r => r.ListData(It.IsAny<LabTestDefinitionListFilter>()))
            .Returns([def]);

        _nilaiTarifRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe<NilaiTarifType>.None);

        var fallbackTarif = new NilaiTarifType(
            "NT-002",
            "TR-002",
            "Leukosit",
            TipeTarifType.Default.ToReff(),
            KelasType.Default.ToReff(),
            60000m,
            []);

        _nilaiTarifRepoMock
            .Setup(r => r.LoadFirstActiveRate(It.Is<ITarifKey>(k => k.TarifId == "TR-002")))
            .Returns(MayBe.From(fallbackTarif));

        var sut = new LabTestDefinitionListHandler(_definitionRepoMock.Object, _nilaiTarifRepoMock.Object);

        // Act
        var result = (await sut.Handle(new LabTestDefinitionListQuery(true), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TarifPrice.Should().Be(60000m);
    }

    [Fact]
    public async Task Handle_MultipleDefinitionsWithSameTarifId_CachesPriceLookup()
    {
        // Arrange
        var def1 = CreateDefinition("LTD0001", "TR-SHARED", "Shared 1");
        var def2 = CreateDefinition("LTD0002", "TR-SHARED", "Shared 2");
        _definitionRepoMock
            .Setup(r => r.ListData(It.IsAny<LabTestDefinitionListFilter>()))
            .Returns([def1, def2]);

        var sharedTarif = new NilaiTarifType(
            "NT-SHARED",
            "TR-SHARED",
            "Shared Test",
            new TipeTarifReff("UMUM", "Umum"),
            new KelasReff("III", "Kelas III"),
            120000m,
            []);

        _nilaiTarifRepoMock
            .Setup(r => r.LoadEntity(It.Is<INilaiTarifCompositKey>(k => k.TarifId == "TR-SHARED")))
            .Returns(MayBe.From(sharedTarif));

        var sut = new LabTestDefinitionListHandler(_definitionRepoMock.Object, _nilaiTarifRepoMock.Object);

        // Act
        var result = (await sut.Handle(new LabTestDefinitionListQuery(true), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].TarifPrice.Should().Be(120000m);
        result[1].TarifPrice.Should().Be(120000m);

        // Verify LoadEntity called only once for TR-SHARED due to per-request caching
        _nilaiTarifRepoMock.Verify(
            r => r.LoadEntity(It.Is<INilaiTarifCompositKey>(k => k.TarifId == "TR-SHARED")),
            Times.Once);
    }
}

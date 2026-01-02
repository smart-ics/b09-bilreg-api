using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class KomponenRepoTests
{
    private readonly Mock<IKomponenDal> _komponenDalMock;
    private readonly Mock<IKomponenSatTugasDal> _komponenSatTugasDalMock;
    private readonly KomponenRepo _repository;

    public KomponenRepoTests()
    {
        _komponenDalMock = new Mock<IKomponenDal>();
        _komponenSatTugasDalMock = new Mock<IKomponenSatTugasDal>();
        _repository = new KomponenRepo(_komponenDalMock.Object, _komponenSatTugasDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _komponenDalMock
            .Setup(x => x.GetData(It.IsAny<IKomponenKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _komponenDalMock.Verify(x => x.Update(It.IsAny<KomponenDto>()), Times.Once);
        _komponenDalMock.Verify(x => x.Insert(It.IsAny<KomponenDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _komponenDalMock
            .Setup(x => x.GetData(key))
            .Returns((KomponenDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _komponenDalMock.Verify(x => x.Insert(It.IsAny<KomponenDto>()), Times.Once);
        _komponenDalMock.Verify(x => x.Update(It.IsAny<KomponenDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var expectedSatTugasDtos = new List<KomponenSatTugasDto> { CreateTestSatTugasDto() };
        var key = CreateTestKey();
        _komponenDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        _komponenSatTugasDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedSatTugasDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model => model.Should().NotBeNull(),
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void UT4_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = CreateTestKey();
        _komponenDalMock
            .Setup(x => x.GetData(key))
            .Returns((KomponenDto)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: model => Assert.Fail("Expected None but got Some"),
            onNone: () => { });
    }

    [Fact]
    public void UT5_GivenKey_WhenDeleteEntity_ThenDeleteIsCalled()
    {
        // Arrange
        var key = CreateTestKey();

        // Act
        _repository.DeleteEntity(key);

        // Assert
        _komponenDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyFilter_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        var filter = new List<IKomponenKey>();

        // Act
        var result = _repository.ListData(filter)?.ToList() ?? [];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenFilterWithItems_WhenListData_ThenFilteredModelsAreReturned()
    {
        // Arrange
        var filter = new List<IKomponenKey> { CreateTestKey() };
        var expectedDtos = new List<KomponenDto> { CreateTestDto() };
        var expectedSatTugasDtos = new List<KomponenSatTugasDto> { CreateTestSatTugasDto() };
        _komponenDalMock
            .Setup(x => x.ListData())
            .Returns(expectedDtos);
        _komponenSatTugasDalMock
            .Setup(x => x.ListData(It.IsAny<IKomponenKey>()))
            .Returns(expectedSatTugasDtos);

        // Act
        var result = _repository.ListData(filter);

        // Assert
        var komponenTypes = result.ToList();
        komponenTypes.Should().NotBeNull();
        komponenTypes.Count.Should().Be(1);
    }

    [Fact]
    public void UT8_GivenKey_WhenLoadEntity_ThenSatTugasListIsLoaded()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var expectedSatTugasDtos = new List<KomponenSatTugasDto> { CreateTestSatTugasDto() };
        var key = CreateTestKey();
        _komponenDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        _komponenSatTugasDalMock
            .Setup(x => x.ListData(key))
            .Returns(expectedSatTugasDtos);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        _komponenSatTugasDalMock.Verify(x => x.ListData(key), Times.Once);
    }

    private static KomponenType CreateTestModel()
        => KomponenType.Default;
    private static KomponenDto CreateTestDto()
        => KomponenDto.FromModel(KomponenType.Default with{ KomponenId = "A"}); 
    private static KomponenSatTugasDto CreateTestSatTugasDto()
        => new KomponenSatTugasDto("A", "B", "C", "D", "E");
    private static IKomponenKey CreateTestKey()
        => KomponenType.Key("A"); 
}
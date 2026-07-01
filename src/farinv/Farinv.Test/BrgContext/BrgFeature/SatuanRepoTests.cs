using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.BrgContext.BrgFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.BrgFeature;

public class SatuanRepoTests
{
    private readonly Mock<ISatuanDal> _satuanDalMock;
    private readonly SatuanRepo _repository;

    public SatuanRepoTests()
    {
        _satuanDalMock = new Mock<ISatuanDal>();
        _repository = new SatuanRepo(_satuanDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _satuanDalMock
            .Setup(x => x.GetData(It.IsAny<ISatuanKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _satuanDalMock.Verify(x => x.Update(It.IsAny<SatuanDto>()), Times.Once);
        _satuanDalMock.Verify(x => x.Insert(It.IsAny<SatuanDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _satuanDalMock
            .Setup(x => x.GetData(key))
            .Returns((SatuanDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _satuanDalMock.Verify(x => x.Insert(It.IsAny<SatuanDto>()), Times.Once);
        _satuanDalMock.Verify(x => x.Update(It.IsAny<SatuanDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _satuanDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);

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
        _satuanDalMock
            .Setup(x => x.GetData(key))
            .Returns((SatuanDto)null!);

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
        _satuanDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _satuanDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<SatuanDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var satuanTypes = result.ToList();
        satuanTypes.Should().NotBeNull();
        satuanTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<SatuanDto> { CreateTestDto(), CreateTestDto() };
        _satuanDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var satuanTypes = result.ToList();
        satuanTypes.Should().NotBeNull();
        satuanTypes.Count.Should().Be(2);
    }

    private static SatuanType CreateTestModel()
        => SatuanType.Default;

    private static SatuanDto CreateTestDto()
        => SatuanDto.FromModel(SatuanType.Default);

    private static ISatuanKey CreateTestKey()
        => SatuanType.Key("S01");
}
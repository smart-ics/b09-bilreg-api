using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.BrgContext.StandardFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.StandardFeature;

public class GolTerapiRepoTests
{
    private readonly Mock<IGolTerapiDal> _golTerapiDalMock;
    private readonly GolTerapiRepo _repository;

    public GolTerapiRepoTests()
    {
        _golTerapiDalMock = new Mock<IGolTerapiDal>();
        _repository = new GolTerapiRepo(_golTerapiDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _golTerapiDalMock
            .Setup(x => x.GetData(It.IsAny<IGolTerapiKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _golTerapiDalMock.Verify(x => x.Update(It.IsAny<GolTerapiDto>()), Times.Once);
        _golTerapiDalMock.Verify(x => x.Insert(It.IsAny<GolTerapiDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _golTerapiDalMock
            .Setup(x => x.GetData(key))
            .Returns((GolTerapiDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _golTerapiDalMock.Verify(x => x.Insert(It.IsAny<GolTerapiDto>()), Times.Once);
        _golTerapiDalMock.Verify(x => x.Update(It.IsAny<GolTerapiDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _golTerapiDalMock
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
        _golTerapiDalMock
            .Setup(x => x.GetData(key))
            .Returns((GolTerapiDto)null!);

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
        _golTerapiDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _golTerapiDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GolTerapiDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var golTerapiTypes = result.ToList();
        golTerapiTypes.Should().NotBeNull();
        golTerapiTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GolTerapiDto> { CreateTestDto(), CreateTestDto() };
        _golTerapiDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var golTerapiTypes = result.ToList();
        golTerapiTypes.Should().NotBeNull();
        golTerapiTypes.Count.Should().Be(2);
    }

    private static GolTerapiType CreateTestModel()
        => GolTerapiType.Default;

    private static GolTerapiDto CreateTestDto()
        => GolTerapiDto.FromModel(GolTerapiType.Default);

    private static IGolTerapiKey CreateTestKey()
        => GolTerapiType.Key("GT01");
}

using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.BrgContext.StandardFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.StandardFeature;

public class OriginalRepoTests
{
    private readonly Mock<IOriginalDal> _originalDalMock;
    private readonly OriginalRepo _repository;

    public OriginalRepoTests()
    {
        _originalDalMock = new Mock<IOriginalDal>();
        _repository = new OriginalRepo(_originalDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _originalDalMock
            .Setup(x => x.GetData(It.IsAny<IOriginalKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _originalDalMock.Verify(x => x.Update(It.IsAny<OriginalDto>()), Times.Once);
        _originalDalMock.Verify(x => x.Insert(It.IsAny<OriginalDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _originalDalMock
            .Setup(x => x.GetData(key))
            .Returns((OriginalDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _originalDalMock.Verify(x => x.Insert(It.IsAny<OriginalDto>()), Times.Once);
        _originalDalMock.Verify(x => x.Update(It.IsAny<OriginalDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _originalDalMock
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
        _originalDalMock
            .Setup(x => x.GetData(key))
            .Returns((OriginalDto)null!);

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
        _originalDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _originalDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<OriginalDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var originalTypes = result.ToList();
        originalTypes.Should().NotBeNull();
        originalTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<OriginalDto> { CreateTestDto(), CreateTestDto() };
        _originalDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var originalTypes = result.ToList();
        originalTypes.Should().NotBeNull();
        originalTypes.Count.Should().Be(2);
    }

    private static OriginalType CreateTestModel()
        => OriginalType.Default;

    private static OriginalDto CreateTestDto()
        => OriginalDto.FromModel(OriginalType.Default);

    private static IOriginalKey CreateTestKey()
        => OriginalType.Key("ORG01");
}

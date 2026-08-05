using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.BrgContext.StandardFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.StandardFeature;

public class KelasTerapiRepoTests
{
    private readonly Mock<IKelasTerapiDal> _kelasTerapiDalMock;
    private readonly KelasTerapiRepo _repository;

    public KelasTerapiRepoTests()
    {
        _kelasTerapiDalMock = new Mock<IKelasTerapiDal>();
        _repository = new KelasTerapiRepo(_kelasTerapiDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _kelasTerapiDalMock
            .Setup(x => x.GetData(It.IsAny<IKelasTerapiKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _kelasTerapiDalMock.Verify(x => x.Update(It.IsAny<KelasTerapiDto>()), Times.Once);
        _kelasTerapiDalMock.Verify(x => x.Insert(It.IsAny<KelasTerapiDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _kelasTerapiDalMock
            .Setup(x => x.GetData(key))
            .Returns((KelasTerapiDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _kelasTerapiDalMock.Verify(x => x.Insert(It.IsAny<KelasTerapiDto>()), Times.Once);
        _kelasTerapiDalMock.Verify(x => x.Update(It.IsAny<KelasTerapiDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _kelasTerapiDalMock
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
        _kelasTerapiDalMock
            .Setup(x => x.GetData(key))
            .Returns((KelasTerapiDto)null!);

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
        _kelasTerapiDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _kelasTerapiDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<KelasTerapiDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var kelasTerapiTypes = result.ToList();
        kelasTerapiTypes.Should().NotBeNull();
        kelasTerapiTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<KelasTerapiDto> { CreateTestDto(), CreateTestDto() };
        _kelasTerapiDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var kelasTerapiTypes = result.ToList();
        kelasTerapiTypes.Should().NotBeNull();
        kelasTerapiTypes.Count.Should().Be(2);
    }

    private static KelasTerapiType CreateTestModel()
        => KelasTerapiType.Default;

    private static KelasTerapiDto CreateTestDto()
        => KelasTerapiDto.FromModel(KelasTerapiType.Default);

    private static IKelasTerapiKey CreateTestKey()
        => KelasTerapiType.Key("KT01");
}

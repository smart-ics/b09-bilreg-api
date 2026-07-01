using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class GroupObatDkRepoTests
{
    private readonly Mock<IGroupObatDkDal> _groupObatDkDalMock;
    private readonly GroupObatDkRepo _repository;

    public GroupObatDkRepoTests()
    {
        _groupObatDkDalMock = new Mock<IGroupObatDkDal>();
        _repository = new GroupObatDkRepo(_groupObatDkDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _groupObatDkDalMock
            .Setup(x => x.GetData(It.IsAny<IGroupObatDkKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _groupObatDkDalMock.Verify(x => x.Update(It.IsAny<GroupObatDkDto>()), Times.Once);
        _groupObatDkDalMock.Verify(x => x.Insert(It.IsAny<GroupObatDkDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _groupObatDkDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupObatDkDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _groupObatDkDalMock.Verify(x => x.Insert(It.IsAny<GroupObatDkDto>()), Times.Once);
        _groupObatDkDalMock.Verify(x => x.Update(It.IsAny<GroupObatDkDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _groupObatDkDalMock
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
        _groupObatDkDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupObatDkDto)null!);

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
        _groupObatDkDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _groupObatDkDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GroupObatDkDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupObatDkTypes = result.ToList();
        groupObatDkTypes.Should().NotBeNull();
        groupObatDkTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GroupObatDkDto> { CreateTestDto(), CreateTestDto() };
        _groupObatDkDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupObatDkTypes = result.ToList();
        groupObatDkTypes.Should().NotBeNull();
        groupObatDkTypes.Count.Should().Be(2);
    }

    private static GroupObatDkType CreateTestModel()
        => GroupObatDkType.Default;

    private static GroupObatDkDto CreateTestDto()
        => GroupObatDkDto.FromModel(GroupObatDkType.Default);

    private static IGroupObatDkKey CreateTestKey()
        => GroupObatDkType.Key("GOD");
}
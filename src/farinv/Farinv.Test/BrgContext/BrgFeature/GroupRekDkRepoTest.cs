using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.BrgContext.BrgFeature;
using FluentAssertions;
using Moq;

namespace Farinv.Test.BrgContext.BrgFeature;

public class GroupRekDkRepoTests
{
    private readonly Mock<IGroupRekDkDal> _groupRekDkDalMock;
    private readonly GroupRekDkRepo _repository;
    public GroupRekDkRepoTests()
    {
        _groupRekDkDalMock = new Mock<IGroupRekDkDal>();
        _repository = new GroupRekDkRepo(_groupRekDkDalMock.Object);
    }
    
    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _groupRekDkDalMock
            .Setup(x => x.GetData(It.IsAny<IGroupRekDkKey>()))
            .Returns(CreateTestDto());
        // Act
        _repository.SaveChanges(existingModel);
        // Assert
        _groupRekDkDalMock.Verify(x => x.Update(It.IsAny<GroupRekDkDto>()), Times.Once);
        _groupRekDkDalMock.Verify(x => x.Insert(It.IsAny<GroupRekDkDto>()), Times.Never);
    }
    
    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _groupRekDkDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupRekDkDto)null!);
        // Act
        _repository.SaveChanges(newModel);
        // Assert
        _groupRekDkDalMock.Verify(x => x.Insert(It.IsAny<GroupRekDkDto>()), Times.Once);
        _groupRekDkDalMock.Verify(x => x.Update(It.IsAny<GroupRekDkDto>()), Times.Never);
    }
    
    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _groupRekDkDalMock
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
        _groupRekDkDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupRekDkDto)null!);
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
        _groupRekDkDalMock.Verify(x => x.Delete(key), Times.Once);
    }
    
    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _groupRekDkDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GroupRekDkDto>)null!);
        // Act
        var result = _repository.ListData();
        // Assert
        var groupRekDkTypes = result.ToList();
        groupRekDkTypes.Should().NotBeNull();
        groupRekDkTypes.Should().BeEmpty();
    }
    
    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GroupRekDkDto> { CreateTestDto(), CreateTestDto() };
        _groupRekDkDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);
        // Act
        var result = _repository.ListData();
        // Assert
        var groupRekDkTypes = result.ToList();
        groupRekDkTypes.Should().NotBeNull();
        groupRekDkTypes.Count.Should().Be(2);
    }
    
    private static GroupRekDkType CreateTestModel()
        => GroupRekDkType.Default;
    
    private static GroupRekDkDto CreateTestDto()
        => GroupRekDkDto.FromModel(GroupRekDkType.Default); 
    
    private static IGroupRekDkKey CreateTestKey()
        => GroupRekDkType.Key("A"); 
}
using Farinv.Application.BrgContext.BrgFeature;
using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.BrgContext.BrgFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Farinv.Test.BrgContext.BrgFeature;

public class GroupRekRepoTests
{
    private readonly Mock<IGroupRekDal> _groupRekDalMock;
    private readonly Mock<IGroupRekDkRepo> _groupRekDkRepoMock;
    private readonly GroupRekRepo _repository;
    public GroupRekRepoTests()
    {
        _groupRekDalMock = new Mock<IGroupRekDal>();
        _groupRekDkRepoMock = new Mock<IGroupRekDkRepo>();
        _repository = new GroupRekRepo(_groupRekDalMock.Object, _groupRekDkRepoMock.Object);
    }
    
    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _groupRekDalMock
            .Setup(x => x.GetData(It.IsAny<IGroupRekKey>()))
            .Returns(CreateTestDto());
        // Act
        _repository.SaveChanges(existingModel);
        // Assert
        _groupRekDalMock.Verify(x => x.Update(It.IsAny<GroupRekDto>()), Times.Once);
        _groupRekDalMock.Verify(x => x.Insert(It.IsAny<GroupRekDto>()), Times.Never);
    }
    
    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _groupRekDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupRekDto)null!);
        // Act
        _repository.SaveChanges(newModel);
        // Assert
        _groupRekDalMock.Verify(x => x.Insert(It.IsAny<GroupRekDto>()), Times.Once);
        _groupRekDalMock.Verify(x => x.Update(It.IsAny<GroupRekDto>()), Times.Never);
    }
    
    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _groupRekDalMock
            .Setup(x => x.GetData(key))
            .Returns(expectedDto);
        var groupRekDk = CreateTestGroupRekDk();
        _groupRekDkRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IGroupRekDkKey>()))
            .Returns(MayBe<GroupRekDkType>.Some(groupRekDk));
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
        _groupRekDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupRekDto)null!);
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
        _groupRekDalMock.Verify(x => x.Delete(key), Times.Once);
    }
    
    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _groupRekDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GroupRekDto>)null!);
        // Act
        var result = _repository.ListData();
        // Assert
        var groupRekTypes = result.ToList();
        groupRekTypes.Should().NotBeNull();
        groupRekTypes.Should().BeEmpty();
    }
    
    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GroupRekDto> { CreateTestDto(), CreateTestDto() };
        _groupRekDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);
        var groupRekDk = CreateTestGroupRekDk();
        _groupRekDkRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IGroupRekDkKey>()))
            .Returns(MayBe<GroupRekDkType>.Some(groupRekDk));
        // Act
        var result = _repository.ListData();
        // Assert
        var groupRekTypes = result.ToList();
        groupRekTypes.Should().NotBeNull();
        groupRekTypes.Count.Should().Be(2);
    }
    
    private static GroupRekType CreateTestModel()
        => GroupRekType.Default;
    
    private static GroupRekDto CreateTestDto()
        => GroupRekDto.FromModel(GroupRekType.Default); 
    
    private static GroupRekDkType CreateTestGroupRekDk()
        => GroupRekDkType.Default;
    
    private static IGroupRekKey CreateTestKey()
        => GroupRekType.Key("A"); 
}
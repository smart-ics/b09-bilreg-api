using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.BillContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public class GroupTarifDkRepo : IGroupTarifDkRepo
{
    private readonly IGroupTarifDkDal _groupTarifDkDal;
    public GroupTarifDkRepo(IGroupTarifDkDal groupTarifDkDal)
    {
        _groupTarifDkDal = groupTarifDkDal;
    }
    public void SaveChanges(GroupTarifDkType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupTarifDkDal.Update(GroupTarifDkDto.FromModel(model)),
                onNone: () => _groupTarifDkDal.Insert(GroupTarifDkDto.FromModel(model)));
    }

    public MayBe<GroupTarifDkType> LoadEntity(IGroupTarifDkKey key)
    {   
        var dto = _groupTarifDkDal.GetData(key);
        if (dto is null)
            return MayBe<GroupTarifDkType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGroupTarifDkKey key)
    {
        _groupTarifDkDal.Delete(key);
    }

    public IEnumerable<GroupTarifDkType> ListData()
    {
        var listDto = _groupTarifDkDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}

public class GroupTarifDkRepoTests
{
    private readonly Mock<IGroupTarifDkDal> _groupTarifDkDalMock;
    private readonly GroupTarifDkRepo _repository;

    public GroupTarifDkRepoTests()
    {
        _groupTarifDkDalMock = new Mock<IGroupTarifDkDal>();
        _repository = new GroupTarifDkRepo(_groupTarifDkDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _groupTarifDkDalMock
            .Setup(x => x.GetData(It.IsAny<IGroupTarifDkKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _groupTarifDkDalMock.Verify(x => x.Update(It.IsAny<GroupTarifDkDto>()), Times.Once);
        _groupTarifDkDalMock.Verify(x => x.Insert(It.IsAny<GroupTarifDkDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _groupTarifDkDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupTarifDkDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _groupTarifDkDalMock.Verify(x => x.Insert(It.IsAny<GroupTarifDkDto>()), Times.Once);
        _groupTarifDkDalMock.Verify(x => x.Update(It.IsAny<GroupTarifDkDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _groupTarifDkDalMock
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
        _groupTarifDkDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupTarifDkDto)null!);

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
        _groupTarifDkDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _groupTarifDkDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GroupTarifDkDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupTarifDkTypes = result.ToList();
        groupTarifDkTypes.Should().NotBeNull();
        groupTarifDkTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GroupTarifDkDto> { CreateTestDto(), CreateTestDto() };
        _groupTarifDkDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupTarifDkTypes = result.ToList();
        groupTarifDkTypes.Should().NotBeNull();
        groupTarifDkTypes.Count.Should().Be(2);
    }

    private static GroupTarifDkType CreateTestModel()
        => GroupTarifDkType.Default;
    private static GroupTarifDkDto CreateTestDto()
        => GroupTarifDkDto.FromModel(GroupTarifDkType.Default); 
    private static IGroupTarifDkKey CreateTestKey()
        => GroupTarifDkType.Key("A"); 
}
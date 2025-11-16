using Bilreg.Application.BillContext.TindakanFeature;
using Bilreg.Domain.BillContext.TindakanFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

public class GroupTarifRepo : IGroupTarifRepo
{
    private readonly IGroupTarifDal _groupTarifDal;
    public GroupTarifRepo(IGroupTarifDal groupTarifDal)
    {
        _groupTarifDal = groupTarifDal;
    }
    public void SaveChanges(GroupTarifType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _groupTarifDal.Update(GroupTarifDto.FromModel(model)),
                onNone: () => _groupTarifDal.Insert(GroupTarifDto.FromModel(model)));
    }

    public MayBe<GroupTarifType> LoadEntity(IGroupTarifKey key)
    {   
        var dto = _groupTarifDal.GetData(key);
        if (dto is null)
            return MayBe<GroupTarifType>.None;
        var model = dto.ToModel();
        return MayBe.From(model);
    }

    public void DeleteEntity(IGroupTarifKey key)
    {
        _groupTarifDal.Delete(key);
    }

    public IEnumerable<GroupTarifType> ListData()
    {
        var listDto = _groupTarifDal.ListData()?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}

public class GroupTarifRepoTests
{
    private readonly Mock<IGroupTarifDal> _groupTarifDalMock;
    private readonly GroupTarifRepo _repository;

    public GroupTarifRepoTests()
    {
        _groupTarifDalMock = new Mock<IGroupTarifDal>();
        _repository = new GroupTarifRepo(_groupTarifDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalled()
    {
        // Arrange
        var existingModel = CreateTestModel();
        _groupTarifDalMock
            .Setup(x => x.GetData(It.IsAny<IGroupTarifKey>()))
            .Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(existingModel);

        // Assert
        _groupTarifDalMock.Verify(x => x.Update(It.IsAny<GroupTarifDto>()), Times.Once);
        _groupTarifDalMock.Verify(x => x.Insert(It.IsAny<GroupTarifDto>()), Times.Never);
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalled()
    {
        // Arrange
        var newModel = CreateTestModel();
        var key = CreateTestKey();
        _groupTarifDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupTarifDto)null!);

        // Act
        _repository.SaveChanges(newModel);

        // Assert
        _groupTarifDalMock.Verify(x => x.Insert(It.IsAny<GroupTarifDto>()), Times.Once);
        _groupTarifDalMock.Verify(x => x.Update(It.IsAny<GroupTarifDto>()), Times.Never);
    }

    [Fact]
    public void UT3_GivenExistingEntity_WhenLoadEntity_ThenEntityIsReturned()
    {
        // Arrange
        var expectedDto = CreateTestDto();
        var key = CreateTestKey();
        _groupTarifDalMock
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
        _groupTarifDalMock
            .Setup(x => x.GetData(key))
            .Returns((GroupTarifDto)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
        result.Match(
            onSome: _ => Assert.Fail("Expected None but got Some"),
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
        _groupTarifDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenEmptyList_WhenListData_ThenEmptyListIsReturned()
    {
        // Arrange
        _groupTarifDalMock
            .Setup(x => x.ListData())
            .Returns((IEnumerable<GroupTarifDto>)null!);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupTarifTypes = result.ToList();
        groupTarifTypes.Should().NotBeNull();
        groupTarifTypes.Should().BeEmpty();
    }

    [Fact]
    public void UT7_GivenListWithItems_WhenListData_ThenListWithModelsIsReturned()
    {
        // Arrange
        var dtos = new List<GroupTarifDto> { CreateTestDto(), CreateTestDto() };
        _groupTarifDalMock
            .Setup(x => x.ListData())
            .Returns(dtos);

        // Act
        var result = _repository.ListData();

        // Assert
        var groupTarifTypes = result.ToList();
        groupTarifTypes.Should().NotBeNull();
        groupTarifTypes.Count.Should().Be(2);
    }

    private static GroupTarifType CreateTestModel()
        => GroupTarifType.Default;
    private static GroupTarifDto CreateTestDto()
        => GroupTarifDto.FromModel(GroupTarifType.Default); 
    private static IGroupTarifKey CreateTestKey()
        => GroupTarifType.Key("A"); 
}